using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>몬스터 공격이 터지는 연출. 발사체 = 빛나는 구 + 바닥 고리, 원형 공격 = 범위 번쩍임 + 퍼지는 고리</summary>
public sealed class MonsterAttackBurst : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private const float Duration = 0.28f;
    private const int RingSegments = 32;

    public void Play(Material material, float radius, float scale)
    {
        StartCoroutine(BurstRoutine(material, radius, scale));
    }

    /// <summary>원형 공격이 터질 때: 범위 전체가 번쩍 했다가 사라지고, 테두리 고리가 바깥으로 퍼짐</summary>
    public void PlayShockwave(Material material, Mesh diskMesh, float radius, Color color)
    {
        StartCoroutine(ShockwaveRoutine(material, diskMesh, radius, color));
    }

    private IEnumerator ShockwaveRoutine(Material material, Mesh diskMesh, float radius, Color color)
    {
        var disk = new GameObject("Flash");
        disk.transform.SetParent(transform, false);
        disk.transform.localScale = new Vector3(radius, 1f, radius);
        disk.AddComponent<MeshFilter>().sharedMesh = diskMesh;
        var diskRenderer = disk.AddComponent<MeshRenderer>();
        diskRenderer.sharedMaterial = material;
        diskRenderer.shadowCastingMode = ShadowCastingMode.Off;
        var properties = new MaterialPropertyBlock();

        LineRenderer ring = CreateRing(material, 0.12f);
        Vector3 center = transform.position;
        float elapsed = 0f;
        const float duration = 0.35f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float fade = 1f - t;

            // 처음엔 밝게(HDR) 번쩍였다가 빠르게 사라짐
            properties.SetColor(BaseColorId, new Color(color.r * 2.5f, color.g * 2.5f + 0.4f, color.b * 2.5f + 0.2f, 0.55f * fade * fade));
            diskRenderer.SetPropertyBlock(properties);

            SetRing(ring, center, radius * Mathf.Lerp(0.3f, 1.15f, 1f - fade * fade));
            ring.startColor = ring.endColor = new Color(1f, 0.55f, 0.3f, fade);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private LineRenderer CreateRing(Material material, float width)
    {
        var ringObject = new GameObject("Ring");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.sharedMaterial = material;
        ring.useWorldSpace = true;
        ring.loop = true;
        ring.alignment = LineAlignment.TransformZ;
        ring.positionCount = RingSegments;
        ring.widthMultiplier = width;
        ring.shadowCastingMode = ShadowCastingMode.Off;
        return ring;
    }

    private static void SetRing(LineRenderer ring, Vector3 center, float ringRadius)
    {
        for (int i = 0; i < RingSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / RingSegments;
            ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius);
        }
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

        LineRenderer ring = CreateRing(material, 0.06f * Mathf.Max(0.5f, scale));

        Vector3 ground = new Vector3(transform.position.x, 0.08f, transform.position.z);
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            float t = elapsed / Duration;
            float fade = 1f - t;

            flash.transform.localScale = Vector3.one * (radius * Mathf.Lerp(2f, 7f, t) * scale);
            properties.SetColor(BaseColorId, new Color(3f, 1.4f, 0.3f, fade * fade));
            flashRenderer.SetPropertyBlock(properties);

            SetRing(ring, ground, Mathf.Lerp(0.1f, 1.1f, 1f - fade * fade) * scale);
            ring.startColor = ring.endColor = new Color(1f, 0.6f, 0.15f, fade * 0.9f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
