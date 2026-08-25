using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossView : MonoBehaviour
{
    [Header("Renderer")]
    [Tooltip("실제 보이는 보스 FBX 오브젝트를 넣으세요. 비워두면 자식 Renderer 중 현재 켜져 있는 것만 자동으로 잡습니다.")]
    [SerializeField] private Transform visualRoot;

    [Header("Hit Blink")]
    [SerializeField] private float blinkDuration = 0.4f;
    [SerializeField] private float blinkInterval = 0.04f;

    private readonly List<Renderer> hitBlinkRenderers = new List<Renderer>();
    private readonly List<bool> originalRendererEnabledStates = new List<bool>();

    private Coroutine hitBlinkCoroutine;

    private void Awake()
    {
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        hitBlinkRenderers.Clear();
        originalRendererEnabledStates.Clear();

        Transform root = visualRoot != null ? visualRoot : transform;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            // 보스 루트 오브젝트에 숨겨둔 Renderer가 있으면 제외
            if (renderer.transform == transform)
            {
                continue;
            }

            // 처음부터 꺼져 있던 Renderer는 깜빡임 대상에서 제외
            if (!renderer.enabled)
            {
                continue;
            }

            hitBlinkRenderers.Add(renderer);
            originalRendererEnabledStates.Add(renderer.enabled);
        }
    }

    public void PlayHitBlinkEffect()
    {
        if (hitBlinkRenderers.Count == 0)
        {
            CacheRenderers();
        }

        if (hitBlinkRenderers.Count == 0)
        {
            Debug.LogWarning("BossView: 깜빡임에 사용할 Renderer가 없습니다.");
            return;
        }

        if (hitBlinkCoroutine != null)
        {
            StopCoroutine(hitBlinkCoroutine);
            RestoreOriginalRendererStates();
        }

        hitBlinkCoroutine = StartCoroutine(HitBlinkRoutine());
    }

    private IEnumerator HitBlinkRoutine()
    {
        float timer = 0f;

        while (timer < blinkDuration)
        {
            SetRenderersVisible(false);
            yield return new WaitForSeconds(blinkInterval);

            SetRenderersVisible(true);
            yield return new WaitForSeconds(blinkInterval);

            timer += blinkInterval * 2f;
        }

        RestoreOriginalRendererStates();
        hitBlinkCoroutine = null;
    }

    private void SetRenderersVisible(bool isVisible)
    {
        for (int i = 0; i < hitBlinkRenderers.Count; i++)
        {
            Renderer renderer = hitBlinkRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = isVisible;
        }
    }

    private void RestoreOriginalRendererStates()
    {
        for (int i = 0; i < hitBlinkRenderers.Count; i++)
        {
            Renderer renderer = hitBlinkRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = originalRendererEnabledStates[i];
        }
    }
}