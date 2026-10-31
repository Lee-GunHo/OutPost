using UnityEngine;

public sealed class MonsterCombatView : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    // HDR 색(1보다 큼)이라 후처리 Bloom에서 빛나 보임
    private static readonly Color ProjectileGlow = new Color(3f, 1.3f, 0.2f, 1f);
    private static readonly Color LaneColor = new Color(1f, 0.55f, 0.05f, 1f);

    private GameObject warning;
    private Material material;

    // 원거리 공격 예고(바닥 경로가 발사 시점까지 차오름 + 몬스터 앞에 모이는 구슬)
    private LineRenderer laneFill;
    private LineRenderer laneArrow;
    private Transform chargeOrb;
    private Vector3 laneStart;
    private Vector3 laneDirection;
    private float laneLength;
    private float projectileRadius;
    private float windupDuration;
    private float windupElapsed;

    public Material AttackMaterial
    {
        get
        {
            if (material == null)
                material = Resources.Load<Material>("MonsterCombat/AttackVisual");
            return material;
        }
    }

    public void ShowWarning(MonsterAttackKind kind, Vector3 origin, Vector3 direction, float radius,
        float windup = 0f, float projectileSize = 0.15f)
    {
        ClearWarning();
        if (kind == MonsterAttackKind.Melee) return;
        if (kind == MonsterAttackKind.Projectile)
        {
            ShowProjectileWarning(origin, direction, radius, windup, projectileSize);
            return;
        }
        warning = new GameObject("Monster " + kind + " warning");
        var line = warning.AddComponent<LineRenderer>();
        line.sharedMaterial = AttackMaterial;
        line.useWorldSpace = true;
        line.widthMultiplier = 0.07f;
        line.startColor = line.endColor = kind == MonsterAttackKind.Circle
            ? new Color(1f, 0.15f, 0.05f, 0.9f)
            : kind == MonsterAttackKind.Charge ? new Color(1f, 0.3f, 0.65f, 0.9f)
            : new Color(1f, 0.65f, 0.05f, 0.9f);
        if (kind == MonsterAttackKind.Circle)
        {
            line.loop = true;
            line.positionCount = 64;
            origin.y = 0.12f;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64f;
                line.SetPosition(i, origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }
        else if (kind == MonsterAttackKind.Charge)
        {
            origin.y = 0.12f;
            Vector3 tip = origin + direction * radius;
            Vector3 side = Vector3.Cross(Vector3.up, direction) * 0.35f;
            line.positionCount = 5;
            line.SetPosition(0, origin);
            line.SetPosition(1, tip);
            line.SetPosition(2, tip - direction * 0.5f + side);
            line.SetPosition(3, tip);
            line.SetPosition(4, tip - direction * 0.5f - side);
        }
        else
        {
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, origin + direction * radius);
        }
    }

    private void ShowProjectileWarning(Vector3 origin, Vector3 direction, float range, float windup, float size)
    {
        warning = new GameObject("Monster Projectile warning");
        laneStart = new Vector3(origin.x, 0.06f, origin.z);
        laneDirection = direction;
        laneLength = range;
        projectileRadius = size;
        windupDuration = Mathf.Max(0.05f, windup);
        windupElapsed = 0f;

        float width = Mathf.Max(0.4f, size * 2f + 0.25f);
        Vector3 end = laneStart + direction * range;

        // 바닥에 깔린 흐린 경로(전체 사거리)
        CreateFlatLine("Lane", width, new Color(LaneColor.r, LaneColor.g, LaneColor.b, 0.16f), laneStart, end);
        // 양쪽 테두리
        Vector3 side = Vector3.Cross(Vector3.up, direction) * (width * 0.5f);
        CreateFlatLine("Edge L", 0.035f, new Color(1f, 0.6f, 0.1f, 0.7f), laneStart + side, end + side);
        CreateFlatLine("Edge R", 0.035f, new Color(1f, 0.6f, 0.1f, 0.7f), laneStart - side, end - side);
        // 발사 시점까지 차오르는 진한 부분
        laneFill = CreateFlatLine("Fill", width, new Color(1f, 0.45f, 0.05f, 0.45f), laneStart, laneStart);
        // 끝의 화살촉(깜빡임)
        laneArrow = CreateFlatLine("Arrow", 0.09f, new Color(1f, 0.8f, 0.3f, 0.9f),
            end - direction * 0.45f + side * 0.9f, end, end - direction * 0.45f - side * 0.9f);

        // 몬스터 앞에 에너지가 모이는 구슬 → 다 모이면 그대로 발사체가 됨
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Charge orb";
        Destroy(orb.GetComponent<Collider>());
        orb.transform.SetParent(warning.transform, false);
        orb.transform.position = origin;
        orb.transform.localScale = Vector3.zero;
        Renderer orbRenderer = orb.GetComponent<Renderer>();
        orbRenderer.sharedMaterial = AttackMaterial;
        orbRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var properties = new MaterialPropertyBlock();
        properties.SetColor(BaseColorId, ProjectileGlow);
        orbRenderer.SetPropertyBlock(properties);
        chargeOrb = orb.transform;

        SfxPlayer.PlayAt(SfxId.ProjectileCharge, origin);
    }

    // 바닥에 눕힌 LineRenderer (TransformZ 정렬 + 위를 보도록 회전해서 폭이 바닥 평면으로 퍼짐)
    private LineRenderer CreateFlatLine(string lineName, float width, Color color, params Vector3[] points)
    {
        var child = new GameObject(lineName);
        child.transform.SetParent(warning.transform, false);
        child.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var line = child.AddComponent<LineRenderer>();
        line.sharedMaterial = AttackMaterial;
        line.useWorldSpace = true;
        line.alignment = LineAlignment.TransformZ;
        line.widthMultiplier = width;
        line.numCapVertices = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.startColor = line.endColor = color;
        line.positionCount = points.Length;
        line.SetPositions(points);
        return line;
    }

    private void Update()
    {
        if (laneFill == null || warning == null) return;

        windupElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(windupElapsed / windupDuration);
        laneFill.SetPosition(1, laneStart + laneDirection * (laneLength * progress));

        // 발사 직전일수록 빠르게 깜빡임
        float blinkSpeed = Mathf.Lerp(6f, 22f, progress);
        float blink = 0.55f + 0.45f * Mathf.Sin(windupElapsed * blinkSpeed);
        Color arrowColor = new Color(1f, 0.8f, 0.3f, 0.9f * blink);
        laneArrow.startColor = laneArrow.endColor = arrowColor;

        if (chargeOrb != null)
        {
            float pulse = 1f + 0.15f * Mathf.Sin(windupElapsed * 30f);
            chargeOrb.localScale = Vector3.one * (projectileRadius * 2.4f * progress * pulse);
        }
    }

    public void ClearWarning()
    {
        laneFill = null;
        laneArrow = null;
        chargeOrb = null;
        if (warning == null) return;
        warning.SetActive(false);
        Destroy(warning);
        warning = null;
    }

    private void OnDisable() { ClearWarning(); }
}
