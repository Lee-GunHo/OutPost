using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 블록을 캘 때 보이는 연출: 맞을 때마다 블록이 "퉁" 눌렸다 튀어나오고, 깎인 내구도만큼 금이 점점 퍼짐(코어키퍼식).
/// BreakableWallPresenter가 처음 맞을 때 블록 루트에 자동으로 붙임 — 프리팹 수정 불필요.
/// 금은 블록 렌더러마다 같은 메시를 한 번 더 그리는 자식(__BlockCrack)으로 표시하고,
/// 단계 파츠(Up/Down)가 지워지면 그 자식도 같이 지워짐.
/// </summary>
public class BreakableWallCrackView : MonoBehaviour
{
    private const string ShaderResourcePath = "GameFeel/BlockCrack";
    private const string CrackObjectName = "__BlockCrack";
    private const int TextureSize = 16;

    [Header("금 가는 효과")]
    [Tooltip("한 대만 맞아도 이만큼은 금이 보이게 함(0~1)")]
    [Range(0f, 1f)] [SerializeField] private float minimumCrack = 0.2f;

    [Header("맞을 때 눌리는 효과")]
    [SerializeField] private float punchDuration = 0.16f;
    [Tooltip("x = 옆으로 퍼지는 비율, y = 위아래로 눌리는 비율")]
    [SerializeField] private Vector2 punchAmount = new Vector2(0.08f, -0.12f);

    private static readonly int CrackId = Shader.PropertyToID("_Crack");
    private static Material crackMaterial;
    private static bool shaderMissingWarned;

    private readonly List<Renderer> crackRenderers = new List<Renderer>();
    private MaterialPropertyBlock propertyBlock;
    private Vector3 originalScale;
    private Coroutine punchCoroutine;
    private bool initialized;

    /// <summary>damageRatio = 깎인 내구도 비율(0~1). showCrack이 false면(나무 등) 눌리는 효과만</summary>
    public void PlayHit(float damageRatio, bool showCrack = true)
    {
        Initialize();
        SetCrack(showCrack ? damageRatio : 0f);

        if (punchCoroutine != null)
            StopCoroutine(punchCoroutine);

        if (isActiveAndEnabled)
            punchCoroutine = StartCoroutine(PunchRoutine());
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        originalScale = transform.localScale;
        propertyBlock = new MaterialPropertyBlock();

        Material material = GetCrackMaterial();

        if (material == null)
            return;

        foreach (Renderer source in GetComponentsInChildren<Renderer>())
        {
            if (!source.enabled || source.name.StartsWith("__") || !(source is MeshRenderer))
                continue;

            MeshFilter filter = source.GetComponent<MeshFilter>();

            if (filter == null || filter.sharedMesh == null)
                continue;

            crackRenderers.Add(CreateCrackRenderer(source, filter.sharedMesh, material));
        }
    }

    private static Renderer CreateCrackRenderer(Renderer source, Mesh mesh, Material material)
    {
        GameObject crackObject = new GameObject(CrackObjectName);
        crackObject.layer = source.gameObject.layer;
        crackObject.transform.SetParent(source.transform, false);
        crackObject.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer meshRenderer = crackObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        meshRenderer.enabled = false;
        return meshRenderer;
    }

    private void SetCrack(float damageRatio)
    {
        float amount = damageRatio <= 0f ? 0f : Mathf.Lerp(minimumCrack, 1f, Mathf.Clamp01(damageRatio));
        propertyBlock.SetFloat(CrackId, amount);

        foreach (Renderer crackRenderer in crackRenderers)
        {
            // 단계 파츠와 함께 지워진 것은 건너뜀
            if (crackRenderer == null)
                continue;

            crackRenderer.SetPropertyBlock(propertyBlock);
            crackRenderer.enabled = amount > 0f;
        }
    }

    // 맞는 순간 납작하게 눌렸다가 살짝 튀어오른 뒤 원래 크기로
    private IEnumerator PunchRoutine()
    {
        float elapsed = 0f;

        while (elapsed < punchDuration)
        {
            float t = elapsed / punchDuration;
            float wave = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t);
            transform.localScale = Vector3.Scale(originalScale,
                new Vector3(1f + punchAmount.x * wave, 1f + punchAmount.y * wave, 1f + punchAmount.x * wave));

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        punchCoroutine = null;
    }

    private void OnDisable()
    {
        if (initialized)
            transform.localScale = originalScale;

        punchCoroutine = null;
    }

    private static Material GetCrackMaterial()
    {
        if (crackMaterial != null)
            return crackMaterial;

        Shader shader = Resources.Load<Shader>(ShaderResourcePath);

        if (shader == null)
            shader = Shader.Find("OutPost/BlockCrack");

        if (shader == null)
        {
            if (!shaderMissingWarned)
            {
                shaderMissingWarned = true;
                Debug.LogWarning("Resources/" + ShaderResourcePath + " 셰이더를 찾지 못해 블록 금 효과를 표시하지 않습니다.");
            }

            return null;
        }

        crackMaterial = new Material(shader) { name = "BlockCrack (Runtime)" };
        crackMaterial.SetTexture("_CrackTex", CreateCrackTexture());
        return crackMaterial;
    }

    /// <summary>
    /// 픽셀 금 텍스처를 코드로 생성. 가운데 근처에서 시작한 금이 가지를 치며 뻗어나가고,
    /// 먼저 그려진 픽셀일수록 R이 작아서 데미지가 적을 때부터 보임. 가장자리는 반대편과 이어져 타일링이 자연스러움.
    /// </summary>
    private static Texture2D CreateCrackTexture()
    {
        System.Random random = new System.Random(7);
        float[] order = new float[TextureSize * TextureSize];

        for (int i = 0; i < order.Length; i++)
            order[i] = -1f;

        int drawn = 0;
        var walkers = new List<(int x, int y, int dx, int dy, int life)>();
        int[,] directions = { { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }, { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 } };

        // 가운데 근처 한 점에서 여러 방향으로 금이 출발
        int startX = TextureSize / 2 + random.Next(-2, 3);
        int startY = TextureSize / 2 + random.Next(-2, 3);

        for (int i = 0; i < 4; i++)
        {
            int dir = (i * 2 + random.Next(0, 2)) % 8;
            walkers.Add((startX, startY, directions[dir, 0], directions[dir, 1], random.Next(6, 11)));
        }

        while (walkers.Count > 0)
        {
            var next = new List<(int x, int y, int dx, int dy, int life)>();

            foreach (var walker in walkers)
            {
                int index = Wrap(walker.y) * TextureSize + Wrap(walker.x);

                if (order[index] < 0f)
                    order[index] = drawn++;

                if (walker.life <= 0)
                    continue;

                int dx = walker.dx;
                int dy = walker.dy;

                // 가끔 방향을 45도 틀어서 지그재그 모양
                if (random.NextDouble() < 0.3)
                {
                    int dir = DirectionIndex(directions, dx, dy) + (random.Next(0, 2) == 0 ? 1 : -1);
                    dir = (dir + 8) % 8;
                    dx = directions[dir, 0];
                    dy = directions[dir, 1];
                }

                next.Add((walker.x + dx, walker.y + dy, dx, dy, walker.life - 1));

                // 가끔 가지를 침
                if (random.NextDouble() < 0.12)
                {
                    int branch = (DirectionIndex(directions, dx, dy) + (random.Next(0, 2) == 0 ? 2 : -2) + 8) % 8;
                    next.Add((walker.x, walker.y, directions[branch, 0], directions[branch, 1], random.Next(2, 5)));
                }
            }

            walkers = next;
        }

        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false, true)
        {
            name = "BlockCrack (Runtime)",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
        };

        var pixels = new Color32[order.Length];

        for (int i = 0; i < order.Length; i++)
        {
            bool isCrack = order[i] >= 0f;
            byte threshold = isCrack ? (byte)Mathf.RoundToInt(order[i] / Mathf.Max(1, drawn - 1) * 255f) : (byte)255;
            pixels[i] = new Color32(threshold, 0, 0, isCrack ? (byte)255 : (byte)0);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static int Wrap(int value)
    {
        return ((value % TextureSize) + TextureSize) % TextureSize;
    }

    private static int DirectionIndex(int[,] directions, int dx, int dy)
    {
        for (int i = 0; i < 8; i++)
        {
            if (directions[i, 0] == dx && directions[i, 1] == dy)
                return i;
        }

        return 0;
    }
}
