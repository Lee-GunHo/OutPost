using UnityEngine;
using UnityEngine.Rendering;

public sealed class MonsterProjectileView : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    // HDR 색(1보다 큼)이라 후처리 Bloom에서 빛나 보임
    private static readonly Color CoreColor = new Color(3f, 1.3f, 0.2f, 1f);
    private static readonly Color HaloColor = new Color(1f, 0.45f, 0.05f, 0.35f);

    private Transform core;
    private Transform halo;
    private TrailRenderer trail;
    private Material material;
    private float radius;
    private float age;

    public void Initialize(float radius, Material material)
    {
        this.radius = radius;
        this.material = material;

        core = CreateSphere("Projectile visual", radius * 2f, CoreColor).transform;
        halo = CreateSphere("Projectile halo", radius * 3.6f, HaloColor).transform;

        // 지나간 자리에 남는 불꼬리
        var trailObject = new GameObject("Projectile trail");
        trailObject.transform.SetParent(transform, false);
        trail = trailObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = material;
        trail.time = 0.2f;
        trail.minVertexDistance = 0.05f;
        trail.widthMultiplier = radius * 2.2f;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.colorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.02f), 1f) },
            alphaKeys = new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) },
        };
    }

    private GameObject CreateSphere(string sphereName, float size, Color color)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = sphereName;
        body.transform.SetParent(transform, false);
        body.transform.localScale = Vector3.one * size;
        Collider collider = body.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        Renderer renderer = body.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        var properties = new MaterialPropertyBlock();
        properties.SetColor(BaseColorId, color);
        renderer.SetPropertyBlock(properties);
        return body;
    }

    private void Update()
    {
        if (core == null) return;

        // 이글거리는 느낌으로 크기가 빠르게 들썩임
        age += Time.deltaTime;
        float pulse = 1f + 0.18f * Mathf.Sin(age * 40f);
        core.localScale = Vector3.one * (radius * 2f * pulse);
        halo.localScale = Vector3.one * (radius * 3.6f * (2f - pulse));
    }

    public void SetPosition(Vector3 position) { transform.position = position; }

    /// <summary>impact = 무언가에 맞음(터지는 효과), false = 사거리 끝에서 사라짐(작게 꺼짐)</summary>
    public void Remove(bool impact = false)
    {
        if (impact)
            SfxPlayer.PlayAt(SfxId.ProjectileImpact, transform.position);

        SpawnBurst(transform.position, impact ? 1f : 0.45f);
        DetachTrail();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    // 꼬리가 발사체와 같이 뚝 끊기지 않게 떼어내서 자연스럽게 사라지도록 둠
    private void DetachTrail()
    {
        if (trail == null) return;
        trail.transform.SetParent(null, true);
        trail.emitting = false;
        trail.autodestruct = true;
    }

    private void SpawnBurst(Vector3 position, float scale)
    {
        if (material == null) return;
        var burst = new GameObject("Projectile burst");
        burst.transform.position = position;
        burst.AddComponent<MonsterAttackBurst>().Play(material, radius, scale);
    }
}
