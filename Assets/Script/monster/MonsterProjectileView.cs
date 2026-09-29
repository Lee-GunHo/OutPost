using UnityEngine;

public sealed class MonsterProjectileView : MonoBehaviour
{
    public void Initialize(float radius, Material material)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Projectile visual";
        body.transform.SetParent(transform, false);
        body.transform.localScale = Vector3.one * radius * 2f;
        Collider collider = body.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        Renderer renderer = body.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", new Color(1f, 0.55f, 0.05f, 1f));
        renderer.SetPropertyBlock(properties);
    }

    public void SetPosition(Vector3 position) { transform.position = position; }
    public void Remove() { gameObject.SetActive(false); Destroy(gameObject); }
}
