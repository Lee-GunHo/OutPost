using UnityEngine;
using UnityEngine.Rendering;

public class BossAoeWarningView : MonoBehaviour
{
    private const int SegmentCount = 96;

    public static BossAoeWarningView Create(Vector3 center, float radius, float duration)
    {
        GameObject warningObject = new GameObject("Boss AOE Warning");

        // 땅이랑 겹쳐서 안 보이는 걸 막기 위해 살짝 위로 올림
        warningObject.transform.position = center + Vector3.up * 0.12f;

        BossAoeWarningView warningView = warningObject.AddComponent<BossAoeWarningView>();
        warningView.Initialize(radius);

        Destroy(warningObject, duration);

        return warningView;
    }

    private void Initialize(float radius)
    {
        CreateFilledCircle(radius);
        CreateCircleOutline(radius);
    }

    private void CreateFilledCircle(float radius)
    {
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = "AOE Filled Circle Mesh";

        Vector3[] vertices = new Vector3[SegmentCount + 1];
        int[] triangles = new int[SegmentCount * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i < SegmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / SegmentCount;

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices[i + 1] = new Vector3(x, 0f, z);
        }

        for (int i = 0; i < SegmentCount; i++)
        {
            int triangleIndex = i * 3;

            int current = i + 1;
            int next = i == SegmentCount - 1 ? 1 : i + 2;

            // 위에서 보이도록 삼각형 방향 수정
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = next;
            triangles[triangleIndex + 2] = current;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;

        meshRenderer.material = CreateTransparentMaterial(new Color(1f, 0f, 0f, 0.28f));
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private void CreateCircleOutline(float radius)
    {
        LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = SegmentCount;
        lineRenderer.widthMultiplier = 0.08f;

        Material lineMaterial = CreateTransparentMaterial(new Color(1f, 0f, 0f, 0.95f));

        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = new Color(1f, 0f, 0f, 0.95f);
        lineRenderer.endColor = new Color(1f, 0f, 0f, 0.95f);

        for (int i = 0; i < SegmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / SegmentCount;

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            lineRenderer.SetPosition(i, new Vector3(x, 0f, z));
        }
    }

    private Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.name = "AOE Warning Material";
        material.color = color;
        material.renderQueue = 3000;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)CullMode.Off);
        }

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");

        return material;
    }
}