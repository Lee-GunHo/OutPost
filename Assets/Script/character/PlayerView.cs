using System.Collections;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private Renderer playerRenderer;

    private Coroutine dashEffectCoroutine;

    private void Awake()
    {
        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }
    }

    public void PlayDashBlinkEffect(float duration)
    {
        if (playerRenderer == null)
        {
            Debug.LogWarning("PlayerView: playerRenderer가 없습니다.");
            return;
        }

        if (dashEffectCoroutine != null)
        {
            StopCoroutine(dashEffectCoroutine);
        }

        dashEffectCoroutine = StartCoroutine(DashBlinkEffect(duration));
    }

    private IEnumerator DashBlinkEffect(float duration)
    {
        float timer = 0f;
        float blinkInterval = 0.08f;

        while (timer < duration)
        {
            playerRenderer.enabled = false;
            yield return new WaitForSeconds(blinkInterval);

            playerRenderer.enabled = true;
            yield return new WaitForSeconds(blinkInterval);

            timer += blinkInterval * 2f;
        }

        playerRenderer.enabled = true;
        dashEffectCoroutine = null;
    }
}