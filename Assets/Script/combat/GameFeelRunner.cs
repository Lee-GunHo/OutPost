using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// HitFeedback의 실제 연출(히트스톱, 화면 흔들림, 데미지 숫자, 파편)을 돌리는 러너.
/// 씬 배치 불필요 — 게임 시작 시 자동 생성(DontDestroyOnLoad).
/// UI가 열려 게임이 멈춘 동안(Time.timeScale == 0)에는 숫자/파편도 같이 멈춤.
/// </summary>
public class GameFeelRunner : MonoBehaviour
{
    // UIState(0/1)와 구분되는 값이라, 히트스톱이 끝날 때 다른 코드가 바꾼 timeScale을 덮어쓰지 않음
    private const float HitStopTimeScale = 0.02f;
    private const int DamageNumberPoolSize = 24;
    private const int DebrisPoolSize = 80;

    private static GameFeelRunner instance;

    // ---- 히트스톱
    private bool hitStopActive;
    private float hitStopEndRealtime;

    // ---- 화면 흔들림
    private float shakeStrength;
    private float shakeSeed;
    private Camera shakenCamera;
    private Vector3 shakeAppliedOffset;

    // ---- 데미지 숫자
    private class DamageNumber
    {
        public TextMeshPro text;
        public Vector3 origin;
        public Vector3 drift;
        public Color color;
        public float age;
        public float scale;
        public bool active;
    }

    private readonly List<DamageNumber> damageNumbers = new List<DamageNumber>();
    private Material damageMaterial;

    // ---- 파편
    private class Debris
    {
        public Transform transform;
        public MeshRenderer renderer;
        public Vector3 velocity;
        public Vector3 spin;
        public float age;
        public float lifetime;
        public float size;
        public bool active;
    }

    private readonly List<Debris> debrisPool = new List<Debris>();
    private Mesh debrisMesh;
    private Material fallbackDebrisMaterial;

    private static HitFeedbackSettings Settings => HitFeedback.Settings;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("GameFeelRunner");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameFeelRunner>();
    }

    private void Awake()
    {
        shakeSeed = Random.Range(0f, 100f);
    }

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        EndHitStop();
    }

    private void Update()
    {
        UpdateHitStop();

        float realDelta = Time.unscaledDeltaTime;
        // UI가 열려 게임이 멈췄으면 연출도 멈춤. 히트스톱 중에는 연출은 계속 움직여야 "멈칫" 느낌이 남
        float effectDelta = Time.timeScale == 0f ? 0f : realDelta;

        // 강하게 시작해서 빠르게 잦아드는 흔들림
        shakeStrength *= Mathf.Exp(-Settings.shakeDecay * effectDelta);
        if (shakeStrength < 0.002f)
            shakeStrength = 0f;
        UpdateDamageNumbers(effectDelta);
        UpdateDebris(effectDelta);
    }

    // ------------------------------------------------------------------ 히트스톱

    public static void HitStop(float duration)
    {
        if (instance == null || duration <= 0f)
            return;

        instance.StartHitStop(duration);
    }

    private void StartHitStop(float duration)
    {
        if (!hitStopActive)
        {
            // UI가 열려 있거나 다른 코드가 시간을 바꿔둔 상태면 건드리지 않음
            if (!Mathf.Approximately(Time.timeScale, 1f))
                return;

            hitStopActive = true;
            Time.timeScale = HitStopTimeScale;
        }

        hitStopEndRealtime = Mathf.Max(hitStopEndRealtime, Time.realtimeSinceStartup + duration);
    }

    private void UpdateHitStop()
    {
        if (hitStopActive && Time.realtimeSinceStartup >= hitStopEndRealtime)
            EndHitStop();
    }

    private void EndHitStop()
    {
        if (!hitStopActive)
            return;

        hitStopActive = false;

        if (Mathf.Approximately(Time.timeScale, HitStopTimeScale))
            Time.timeScale = 1f;
    }

    // ------------------------------------------------------------------ 화면 흔들림

    public static void Shake(float strength)
    {
        if (instance == null || strength <= 0f)
            return;

        // 여러 번 맞으면 조금씩 쌓이지만 무한히 커지지는 않음
        float stacked = Mathf.Max(instance.shakeStrength, strength) + strength * 0.25f;
        instance.shakeStrength = Mathf.Min(stacked, Mathf.Max(strength, 1f));
    }

    // CameraManager는 LateUpdate에서 현재 위치 기준으로 따라가므로, 위치를 직접 바꾸면 흔들림이 누적됨.
    // 그래서 렌더링 직전에만 흔들고 렌더링이 끝나면 원래 위치로 되돌림.
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (shakeStrength <= 0.0001f || cam != Camera.main || shakenCamera != null)
            return;

        float t = Time.unscaledTime * Settings.shakeFrequency;
        // PerlinNoise는 대부분 0.3~0.7 사이라 대략 -1~1이 되게 넓힘
        float x = Mathf.Clamp((Mathf.PerlinNoise(shakeSeed, t) - 0.5f) * 3f, -1f, 1f);
        float y = Mathf.Clamp((Mathf.PerlinNoise(shakeSeed + 37.1f, t) - 0.5f) * 3f, -1f, 1f);
        shakeAppliedOffset = (cam.transform.right * x + cam.transform.up * y) * shakeStrength;
        cam.transform.position += shakeAppliedOffset;
        shakenCamera = cam;
    }

    private void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (cam != shakenCamera)
            return;

        if (cam != null)
            cam.transform.position -= shakeAppliedOffset;

        shakenCamera = null;
        shakeAppliedOffset = Vector3.zero;
    }

    // ------------------------------------------------------------------ 데미지 숫자

    public static void ShowDamageNumber(Vector3 position, int damage, Color color, bool big)
    {
        if (instance != null)
            instance.SpawnDamageNumber(position, damage, color, big);
    }

    private void SpawnDamageNumber(Vector3 position, int damage, Color color, bool big)
    {
        DamageNumber number = GetDamageNumber();

        if (number == null)
            return;

        Vector2 jitter = Random.insideUnitCircle * 0.25f;
        number.origin = position + new Vector3(jitter.x, 0f, jitter.y);
        number.drift = new Vector3(Random.Range(-0.35f, 0.35f), 0f, 0f);
        number.color = color;
        number.age = 0f;
        number.scale = big ? 1.4f : 1f;
        number.active = true;
        number.text.text = damage.ToString();
        number.text.color = color;
        number.text.gameObject.SetActive(true);
        PlaceDamageNumber(number);
    }

    private DamageNumber GetDamageNumber()
    {
        DamageNumber oldest = null;

        foreach (DamageNumber number in damageNumbers)
        {
            if (!number.active)
                return number;

            if (oldest == null || number.age > oldest.age)
                oldest = number;
        }

        if (damageNumbers.Count < DamageNumberPoolSize)
        {
            DamageNumber created = CreateDamageNumber();

            if (created != null)
                damageNumbers.Add(created);

            return created;
        }

        return oldest;
    }

    private DamageNumber CreateDamageNumber()
    {
        TMP_FontAsset font = Settings.damageFont != null ? Settings.damageFont : TMP_Settings.defaultFontAsset;

        if (font == null)
            return null;

        GameObject go = new GameObject("DamageNumber");
        go.transform.SetParent(transform, false);
        go.SetActive(false);

        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.font = font;
        text.fontSize = Settings.damageFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.fontSharedMaterial = GetDamageMaterial(font);
        text.rectTransform.sizeDelta = new Vector2(4f, 1.5f);

        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return new DamageNumber { text = text };
    }

    // 모든 숫자가 같이 쓰는 검은 외곽선 머티리얼(오브젝트마다 머티리얼이 복제되지 않게)
    private Material GetDamageMaterial(TMP_FontAsset font)
    {
        if (damageMaterial != null)
            return damageMaterial;

        damageMaterial = new Material(font.material);

        // 벽 뒤에 가려지지 않게 항상 위에 그림(Overlay 셰이더가 없으면 일반 셰이더로 그림)
        if (Settings.damageNumberShader != null)
            damageMaterial.shader = Settings.damageNumberShader;

        damageMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
        damageMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
        damageMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.08f, 0.05f, 0.05f, 1f));
        damageMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
        damageMaterial.renderQueue = (int)RenderQueue.Overlay;
        return damageMaterial;
    }

    private void UpdateDamageNumbers(float deltaTime)
    {
        foreach (DamageNumber number in damageNumbers)
        {
            if (!number.active)
                continue;

            number.age += deltaTime;

            if (number.age >= Settings.damageNumberLifetime)
            {
                number.active = false;
                number.text.gameObject.SetActive(false);
                continue;
            }

            PlaceDamageNumber(number);
        }
    }

    private void PlaceDamageNumber(DamageNumber number)
    {
        float t = Mathf.Clamp01(number.age / Settings.damageNumberLifetime);
        float rise = 1f - (1f - t) * (1f - t); // 빠르게 튀어올랐다가 천천히 멈춤
        // 처음에 크게 "팍" 튀어나왔다가 원래 크기로
        float pop = number.age < 0.08f ? Mathf.Lerp(1.8f, 1f, number.age / 0.08f) : 1f;
        float alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;

        Transform tr = number.text.transform;
        tr.position = number.origin + Vector3.up * (Settings.damageNumberRise * rise) + number.drift * rise;
        tr.localScale = Vector3.one * (number.scale * pop);

        Camera cam = Camera.main;

        if (cam != null)
            tr.rotation = cam.transform.rotation;

        Color color = number.color;
        color.a = alpha;
        number.text.color = color;
    }

    // ------------------------------------------------------------------ 파편

    /// <summary>source 렌더러의 머티리얼(블록 색)을 그대로 쓰는 작은 큐브 조각을 튀김</summary>
    public static void SpawnDebris(Vector3 position, int count, Renderer source, float power = 1f)
    {
        if (instance != null && count > 0)
            instance.EmitDebris(position, count, source, power);
    }

    private void EmitDebris(Vector3 position, int count, Renderer source, float power)
    {
        Material material = source != null ? source.sharedMaterial : null;

        if (material == null)
            material = GetFallbackDebrisMaterial();

        if (material == null)
            return;

        for (int i = 0; i < count; i++)
        {
            Debris debris = GetDebris();

            if (debris == null)
                return;

            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = Settings.debrisSpeed * power * Random.Range(0.5f, 1f);

            debris.velocity = new Vector3(dir.x * speed * 0.6f, speed * Random.Range(0.7f, 1.2f), dir.y * speed * 0.6f);
            debris.spin = Random.insideUnitSphere * 720f;
            debris.age = 0f;
            debris.lifetime = Settings.debrisLifetime * Random.Range(0.7f, 1.2f);
            debris.size = Settings.debrisSize * Random.Range(0.6f, 1.3f) * Mathf.Lerp(1f, power, 0.5f);
            debris.active = true;

            debris.renderer.sharedMaterial = material;
            debris.transform.position = position + Random.insideUnitSphere * 0.2f;
            debris.transform.rotation = Random.rotation;
            debris.transform.localScale = Vector3.one * debris.size;
            debris.transform.gameObject.SetActive(true);
        }
    }

    private Debris GetDebris()
    {
        Debris oldest = null;

        foreach (Debris debris in debrisPool)
        {
            if (!debris.active)
                return debris;

            if (oldest == null || debris.age / debris.lifetime > oldest.age / oldest.lifetime)
                oldest = debris;
        }

        if (debrisPool.Count < DebrisPoolSize)
        {
            Debris created = CreateDebris();
            debrisPool.Add(created);
            return created;
        }

        return oldest;
    }

    private Debris CreateDebris()
    {
        if (debrisMesh == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debrisMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
        }

        // 콜라이더 없는 큐브(물리/공격 판정에 끼지 않게)
        GameObject go = new GameObject("Debris");
        go.transform.SetParent(transform, false);
        go.SetActive(false);
        go.AddComponent<MeshFilter>().sharedMesh = debrisMesh;

        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        return new Debris { transform = go.transform, renderer = meshRenderer };
    }

    private Material GetFallbackDebrisMaterial()
    {
        if (fallbackDebrisMaterial != null)
            return fallbackDebrisMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            return null;

        fallbackDebrisMaterial = new Material(shader);
        fallbackDebrisMaterial.color = new Color(0.55f, 0.5f, 0.45f);
        return fallbackDebrisMaterial;
    }

    private void UpdateDebris(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        const float gravity = 22f;
        float floor = Settings.floorHeight;

        foreach (Debris debris in debrisPool)
        {
            if (!debris.active)
                continue;

            debris.age += deltaTime;

            if (debris.age >= debris.lifetime)
            {
                debris.active = false;
                debris.transform.gameObject.SetActive(false);
                continue;
            }

            debris.velocity.y -= gravity * deltaTime;
            Vector3 position = debris.transform.position + debris.velocity * deltaTime;
            float halfSize = debris.size * 0.5f;

            // 바닥에 닿으면 통통 튀다가 멈춤
            if (position.y < floor + halfSize)
            {
                position.y = floor + halfSize;

                if (debris.velocity.y < 0f)
                {
                    debris.velocity.y = -debris.velocity.y * 0.35f;
                    debris.velocity.x *= 0.6f;
                    debris.velocity.z *= 0.6f;
                    debris.spin *= 0.5f;
                }
            }

            debris.transform.position = position;
            debris.transform.Rotate(debris.spin * deltaTime, Space.World);

            // 마지막 35% 동안 작아지면서 사라짐
            float remain = 1f - debris.age / debris.lifetime;
            float shrink = remain < 0.35f ? remain / 0.35f : 1f;
            debris.transform.localScale = Vector3.one * (debris.size * shrink);
        }
    }
}
