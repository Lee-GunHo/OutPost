using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 몬스터 공격 예고(windup) 연출. 근접 공격은 예고 없음.
/// 원거리/돌진 = 바닥 경로가 발사 시점까지 차오름, 원형 = 바닥 원이 가운데부터 차오름.
/// 모든 연출은 Resources/MonsterCombat/AttackVisual 머티리얼(정점 색 × _BaseColor, 반투명 unlit) 하나로 그림.
/// </summary>
public sealed class MonsterCombatView : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    // HDR 색(1보다 큼)이라 후처리 Bloom에서 빛나 보임
    private static readonly Color ProjectileGlow = new Color(3f, 1.3f, 0.2f, 1f);
    private static readonly Color ProjectileColor = new Color(1f, 0.5f, 0.05f, 1f);
    private static readonly Color CircleColor = new Color(1f, 0.15f, 0.05f, 1f);
    private static readonly Color ChargeColor = new Color(1f, 0.3f, 0.65f, 1f);

    private const float GroundHeight = 0.06f;
    private const int DiskSegments = 48;
    private const int ChevronCount = 3;
    private const float ChargeLaneWidth = 0.9f;

    private static Mesh diskMesh;

    private GameObject warning;
    private Material material;

    private MonsterAttackKind warningKind;
    private Color warningColor;
    private float windupDuration;
    private float windupElapsed;

    // 원거리/돌진: 바닥 경로
    private LineRenderer laneFill;
    private LineRenderer[] laneChevrons;
    private Vector3 laneStart;
    private Vector3 laneDirection;
    private float laneLength;

    // 원거리: 몬스터 앞에 모이는 구슬
    private Transform chargeOrb;
    private float projectileRadius;

    // 원형: 바닥 원
    private Transform circleFill;
    private LineRenderer circleBorder;
    private float circleRadius;

    public Material AttackMaterial
    {
        get
        {
            if (material == null)
                material = Resources.Load<Material>("MonsterCombat/AttackVisual");
            return material;
        }
    }

    /// <param name="radius">원형 = 반경, 돌진 = 돌진 거리, 원거리 = 사거리</param>
    /// <param name="windup">발사까지 남은 시간(예고가 이 시간 동안 차오름)</param>
    /// <param name="projectileSize">원거리 발사체 반지름</param>
    public void ShowWarning(MonsterAttackKind kind, Vector3 origin, Vector3 direction, float radius,
        float windup = 0f, float projectileSize = 0.15f)
    {
        ClearWarning();
        if (kind == MonsterAttackKind.Melee) return;

        warning = new GameObject("Monster " + kind + " warning");
        warningKind = kind;
        windupDuration = Mathf.Max(0.05f, windup);
        windupElapsed = 0f;

        if (kind == MonsterAttackKind.Circle)
        {
            ShowCircleWarning(origin, radius);
        }
        else if (kind == MonsterAttackKind.Charge)
        {
            ShowLaneWarning(origin, direction, radius, ChargeLaneWidth, ChargeColor);
            SfxPlayer.PlayAt(SfxId.ChargeWindup, origin);
        }
        else
        {
            ShowLaneWarning(origin, direction, radius, Mathf.Max(0.4f, projectileSize * 2f + 0.25f), ProjectileColor);
            ShowChargeOrb(origin, projectileSize);
            SfxPlayer.PlayAt(SfxId.ProjectileCharge, origin);
        }
    }

    /// <summary>예고가 끝나고 실제 공격이 나가는 순간(MonsterCombatPresenter가 ClearWarning 직후 호출)</summary>
    public void PlayRelease(MonsterAttackKind kind, Vector3 origin, float radius)
    {
        if (kind == MonsterAttackKind.Circle)
        {
            SfxPlayer.PlayAt(SfxId.AoeBlast, origin);
            var burst = new GameObject("Monster Circle burst");
            burst.transform.position = new Vector3(origin.x, GroundHeight, origin.z);
            burst.AddComponent<MonsterAttackBurst>().PlayShockwave(AttackMaterial, GetDiskMesh(), radius, CircleColor);
        }
        else if (kind == MonsterAttackKind.Charge)
        {
            SfxPlayer.PlayAt(SfxId.Dash, origin);
        }
    }

    // ---------------------------------------------------------------- 원거리/돌진: 바닥 경로

    private void ShowLaneWarning(Vector3 origin, Vector3 direction, float length, float width, Color color)
    {
        warningColor = color;
        laneStart = new Vector3(origin.x, GroundHeight, origin.z);
        laneDirection = direction;
        laneLength = length;

        Vector3 end = laneStart + direction * length;
        Vector3 side = Vector3.Cross(Vector3.up, direction) * (width * 0.5f);

        // 바닥에 깔린 흐린 경로(전체 길이)와 양쪽 테두리
        CreateFlatLine("Lane", width, WithAlpha(color, 0.16f), laneStart, end);
        CreateFlatLine("Edge L", 0.035f, WithAlpha(Brighten(color), 0.7f), laneStart + side, end + side);
        CreateFlatLine("Edge R", 0.035f, WithAlpha(Brighten(color), 0.7f), laneStart - side, end - side);
        // 발사 시점까지 차오르는 진한 부분
        laneFill = CreateFlatLine("Fill", width, WithAlpha(color, 0.42f), laneStart, laneStart);

        // 진행 방향을 가리키는 화살촉(돌진은 여러 개가 앞으로 흐르듯 깜빡임)
        int chevrons = warningKind == MonsterAttackKind.Charge ? ChevronCount : 1;
        laneChevrons = new LineRenderer[chevrons];

        for (int i = 0; i < chevrons; i++)
        {
            float along = chevrons == 1 ? length : length * (i + 1f) / chevrons;
            Vector3 tip = laneStart + direction * along;
            Vector3 back = tip - direction * Mathf.Min(0.45f, width * 0.6f);
            laneChevrons[i] = CreateFlatLine("Arrow " + i, 0.09f, WithAlpha(Brighten(color), 0.9f),
                back + side * 0.9f, tip, back - side * 0.9f);
        }
    }

    // 몬스터 앞에 에너지가 모이는 구슬 → 다 모이면 그대로 발사체가 됨
    private void ShowChargeOrb(Vector3 origin, float size)
    {
        projectileRadius = size;

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Charge orb";
        Destroy(orb.GetComponent<Collider>());
        orb.transform.SetParent(warning.transform, false);
        orb.transform.position = origin;
        orb.transform.localScale = Vector3.zero;
        Renderer orbRenderer = orb.GetComponent<Renderer>();
        orbRenderer.sharedMaterial = AttackMaterial;
        orbRenderer.shadowCastingMode = ShadowCastingMode.Off;
        var properties = new MaterialPropertyBlock();
        properties.SetColor(BaseColorId, ProjectileGlow);
        orbRenderer.SetPropertyBlock(properties);
        chargeOrb = orb.transform;
    }

    // ---------------------------------------------------------------- 원형: 바닥 원

    private void ShowCircleWarning(Vector3 origin, float radius)
    {
        warningColor = CircleColor;
        circleRadius = radius;
        Vector3 center = new Vector3(origin.x, GroundHeight, origin.z);

        // 범위 전체(흐림) + 가운데부터 커지는 진한 원 + 테두리
        CreateDisk("Area", center, radius, WithAlpha(CircleColor, 0.14f));
        circleFill = CreateDisk("Fill", center + Vector3.up * 0.005f, 0f, WithAlpha(CircleColor, 0.38f)).transform;

        var border = new Vector3[DiskSegments];
        for (int i = 0; i < DiskSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / DiskSegments;
            border[i] = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }

        circleBorder = CreateFlatLine("Border", 0.06f, WithAlpha(Brighten(CircleColor), 0.85f), border);
        circleBorder.loop = true;

        SfxPlayer.PlayAt(SfxId.AoeCharge, origin);
    }

    private GameObject CreateDisk(string diskName, Vector3 center, float radius, Color color)
    {
        var disk = new GameObject(diskName);
        disk.transform.SetParent(warning.transform, false);
        disk.transform.position = center;
        disk.transform.localScale = new Vector3(radius, 1f, radius);
        disk.AddComponent<MeshFilter>().sharedMesh = GetDiskMesh();
        var diskRenderer = disk.AddComponent<MeshRenderer>();
        diskRenderer.sharedMaterial = AttackMaterial;
        diskRenderer.shadowCastingMode = ShadowCastingMode.Off;
        diskRenderer.receiveShadows = false;
        var properties = new MaterialPropertyBlock();
        properties.SetColor(BaseColorId, color);
        diskRenderer.SetPropertyBlock(properties);
        return disk;
    }

    // 반지름 1, 바닥(XZ)에 누운 원. 정점 색은 흰색이라 색은 _BaseColor로만 정함
    private static Mesh GetDiskMesh()
    {
        if (diskMesh != null)
            return diskMesh;

        var vertices = new Vector3[DiskSegments + 1];
        var colors = new Color[DiskSegments + 1];
        var triangles = new int[DiskSegments * 3];
        colors[0] = Color.white;

        for (int i = 0; i < DiskSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / DiskSegments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            colors[i + 1] = Color.white;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = (i + 1) % DiskSegments + 1;
            triangles[i * 3 + 2] = i + 1;
        }

        diskMesh = new Mesh { name = "Monster warning disk", vertices = vertices, colors = colors, triangles = triangles };
        diskMesh.RecalculateBounds();
        return diskMesh;
    }

    // ---------------------------------------------------------------- 공통

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
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.startColor = line.endColor = color;
        line.positionCount = points.Length;
        line.SetPositions(points);
        return line;
    }

    private void Update()
    {
        if (warning == null) return;

        windupElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(windupElapsed / windupDuration);
        // 발사 직전일수록 빠르게 깜빡임
        float blinkSpeed = Mathf.Lerp(6f, 22f, progress);

        if (laneFill != null)
            laneFill.SetPosition(1, laneStart + laneDirection * (laneLength * progress));

        if (laneChevrons != null)
        {
            for (int i = 0; i < laneChevrons.Length; i++)
            {
                // 여러 개면 앞쪽 화살촉이 조금씩 늦게 켜져서 앞으로 흐르는 것처럼 보임
                float phase = windupElapsed * blinkSpeed - i * 1.2f;
                float blink = 0.55f + 0.45f * Mathf.Sin(phase);
                Color color = WithAlpha(Brighten(warningColor), 0.9f * blink);
                laneChevrons[i].startColor = laneChevrons[i].endColor = color;
            }
        }

        if (chargeOrb != null)
        {
            float pulse = 1f + 0.15f * Mathf.Sin(windupElapsed * 30f);
            chargeOrb.localScale = Vector3.one * (projectileRadius * 2.4f * progress * pulse);
        }

        if (circleFill != null)
        {
            float r = circleRadius * progress;
            circleFill.localScale = new Vector3(r, 1f, r);
        }

        if (circleBorder != null)
        {
            float blink = 0.55f + 0.45f * Mathf.Sin(windupElapsed * blinkSpeed);
            circleBorder.startColor = circleBorder.endColor = WithAlpha(Brighten(CircleColor), 0.85f * blink);
        }
    }

    public void ClearWarning()
    {
        laneFill = null;
        laneChevrons = null;
        chargeOrb = null;
        circleFill = null;
        circleBorder = null;
        if (warning == null) return;
        warning.SetActive(false);
        Destroy(warning);
        warning = null;
    }

    private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }

    // 테두리/화살촉용으로 조금 밝게
    private static Color Brighten(Color color) => Color.Lerp(color, Color.white, 0.25f);

    private void OnDisable() { ClearWarning(); }
}
