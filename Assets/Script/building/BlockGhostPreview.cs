using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 설치할 블록이 들어설 자리를 반투명 상자로 보여주는 미리보기.
/// 상자 크기/위치는 설치 프리팹의 메시 범위(없으면 비트리거 콜라이더 범위)로 계산해서 블록 모양과 맞춘다.
/// 콜라이더가 없어 설치 판정(Physics.CheckBox)이나 클릭을 막지 않는다.
/// </summary>
public class BlockGhostPreview
{
    private readonly GameObject ghost;
    private readonly Material material;
    private readonly bool ownsMaterial;
    private readonly float padding;
    private readonly Dictionary<GameObject, Bounds> boundsCache = new Dictionary<GameObject, Bounds>();

    public BlockGhostPreview(Material sharedMaterial, float padding)
    {
        this.padding = padding;

        ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ghost.name = "BlockGhostPreview";
        ghost.layer = 2; // Ignore Raycast
        Object.DestroyImmediate(ghost.GetComponent<Collider>());

        ownsMaterial = sharedMaterial == null;
        material = ownsMaterial ? CreateTransparentMaterial() : new Material(sharedMaterial);

        MeshRenderer renderer = ghost.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ghost.SetActive(false);
    }

    /// <param name="blockPosition">블록 프리팹이 실제로 생성될 위치(셀 좌표 + placeableYOffset)</param>
    public void Show(GameObject prefab, Vector3 blockPosition, Color color)
    {
        if (ghost == null || prefab == null)
        {
            Hide();
            return;
        }

        Bounds bounds = GetLocalBounds(prefab);
        ghost.transform.position = blockPosition + bounds.center;
        ghost.transform.localScale = bounds.size + Vector3.one * padding;
        SetColor(color);
        ghost.SetActive(true);
    }

    public void Hide()
    {
        if (ghost != null)
            ghost.SetActive(false);
    }

    public void Dispose()
    {
        if (ghost != null)
            Object.Destroy(ghost);

        if (material != null)
            Object.Destroy(material);
    }

    private void SetColor(Color color)
    {
        material.color = color;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
    }

    /// <summary>
    /// 프리팹 루트 기준 범위. 프리팹 에셋은 씬에 없어서 Renderer.bounds를 못 쓰므로 메시 범위를 직접 변환.
    /// </summary>
    private Bounds GetLocalBounds(GameObject prefab)
    {
        if (boundsCache.TryGetValue(prefab, out Bounds cached))
            return cached;

        // Instantiate는 루트 스케일은 유지하고 위치/회전만 바꾸므로, 위치와 회전만 제거한 공간에서 계산
        Transform root = prefab.transform;
        Matrix4x4 toRoot =
            Matrix4x4.Rotate(Quaternion.Inverse(root.rotation)) *
            Matrix4x4.Translate(-root.position);
        bool hasBounds = false;
        Bounds result = default;

        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.GetComponent<MeshRenderer>() == null)
                continue;

            // UI(상호작용 표시 캔버스 등)는 제외
            if (filter.GetComponentInParent<Canvas>(true) != null)
                continue;

            Encapsulate(ref result, ref hasBounds, filter.sharedMesh.bounds, toRoot * filter.transform.localToWorldMatrix);
        }

        if (!hasBounds)
        {
            foreach (BoxCollider box in prefab.GetComponentsInChildren<BoxCollider>(true))
            {
                if (box.isTrigger)
                    continue;

                Encapsulate(ref result, ref hasBounds, new Bounds(box.center, box.size), toRoot * box.transform.localToWorldMatrix);
            }
        }

        if (!hasBounds)
            result = new Bounds(Vector3.zero, Vector3.one);

        boundsCache[prefab] = result;
        return result;
    }

    private static void Encapsulate(ref Bounds result, ref bool hasBounds, Bounds local, Matrix4x4 matrix)
    {
        Vector3 min = local.min;
        Vector3 max = local.max;

        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                (i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y,
                (i & 4) == 0 ? min.z : max.z);

            Vector3 point = matrix.MultiplyPoint3x4(corner);

            if (!hasBounds)
            {
                result = new Bounds(point, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(point);
            }
        }
    }

    // URP Unlit을 반투명(Alpha 블렌드)으로 설정
    private static Material CreateTransparentMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material transparent = new Material(shader) { name = "BlockGhostMaterial" };

        if (transparent.HasProperty("_Surface"))
        {
            transparent.SetFloat("_Surface", 1f);
            transparent.SetFloat("_Blend", 0f);
            transparent.SetOverrideTag("RenderType", "Transparent");
            transparent.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            transparent.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            transparent.SetInt("_ZWrite", 0);
            transparent.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        transparent.renderQueue = (int)RenderQueue.Transparent;
        return transparent;
    }
}
