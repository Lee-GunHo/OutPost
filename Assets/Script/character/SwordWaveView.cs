using UnityEngine;
using UnityEngine.Rendering;

public sealed class SwordWaveView : MonoBehaviour
{
    private Mesh mesh;
    private Material material;

    public void Initialize(Vector3 direction)
    {
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        const int segments = 24;
        var vertices = new Vector3[(segments + 1) * 2];
        var triangles = new int[segments * 12];
        for (int i = 0; i <= segments; i++)
        {
            float angle = -Mathf.PI * 0.5f + Mathf.PI * i / segments;
            float curve = Mathf.Cos(angle);
            vertices[i * 2] = new Vector3(Mathf.Sin(angle) * 0.8f, 0f, curve * 0.6f);
            vertices[i * 2 + 1] = vertices[i * 2] - Vector3.forward * (curve * 0.3f);
            if (i == segments) continue;
            int a = i * 2, t = i * 12;
            // Both faces: visible above and below the horizontal crescent.
            int[] face = { a, a + 2, a + 1, a + 1, a + 2, a + 3,
                a + 1, a + 2, a, a + 3, a + 2, a + 1 };
            for (int j = 0; j < face.Length; j++) triangles[t + j] = face[j];
        }
        mesh = new Mesh { name = "Sword wave crescent" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        if (shader == null) return;
        material = new Material(shader);
        Color color = new Color(1.2f, 2.8f, 3.2f, 1f);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        var renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    public void SetPosition(Vector3 position) { transform.position = position; }
    public void Remove(bool impact)
    {
        if (impact) SfxPlayer.PlayAt(SfxId.ProjectileImpact, transform.position);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}
