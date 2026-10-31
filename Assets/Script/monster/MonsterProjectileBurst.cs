using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>발사체가 터질 때: 빛나는 구가 커지며 사라지고, 바닥에 고리가 퍼짐</summary>
public sealed class MonsterProjectileBurst : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private const float Duration = 0.28f;
    private const int RingSegments = 32;

    public void Play(Material material, float radius, float scale)
    {
        StartCoroutine(BurstRoutine(material, radius, scale));
    }

    private IEnumerator BurstRoutine(Material material, float radius, float scale)
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(flash.GetComponent<Collider>());
        flash.transform.SetParent(transform, false);
        Renderer flashRenderer = flash.GetComponent<Renderer>();
        flashRenderer.sharedMaterial = material;
        flashRenderer.shadowCastingMode = ShadowCastingMode.Off;
        var properties = new MaterialPropertyBlock();

        var ringObject = new GameObject("Ring");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.sharedMaterial = material;
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.alignment = LineAlignment.TransformZ;
        ring.positionCount = RingSegments;
        ring.widthMultiplier = 0.06f * Mathf.Max(0.5f, scale);
        ring.shadowCastingMode = ShadowCastingMode.Off;

        Vector3 ground = new Vector3(transform.position.x, 0.08f, transform.position.z);
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            float t = elapsed / Duration;
            float fade = 1f - t;

            flash.transform.localScale = Vector3.one * (radius * Mathf.Lerp(2f, 7f, t) * scale);
            properties.SetColor(BaseColorId, new Color(3f, 1.4f, 0.3f, fade * fade));
            flashRenderer.SetPropertyBlock(properties);

            float ringRadius = Mathf.Lerp(0.1f, 1.1f, 1f - fade * fade) * scale;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                ring.SetPosition(i, ground + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius);
            }
            ring.startColor = ring.endColor = new Color(1f, 0.6f, 0.15f, fade * 0.9f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
