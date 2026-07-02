using System.Collections;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private Renderer playerRenderer;

    [Header("Dash Effect")]
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private Transform dashEffectSpawnPoint;

    private Coroutine hitEffectCoroutine;

    private void Awake()
    {
        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }

        if (dashEffectSpawnPoint == null)
        {
            dashEffectSpawnPoint = transform;
        }
    }

    public void PlayHitBlinkEffect(float duration)
    {
        if (playerRenderer == null)
        {
            Debug.LogWarning("PlayerView: playerRenderer가 없습니다.");
            return;
        }

        if (hitEffectCoroutine != null)
        {
            StopCoroutine(hitEffectCoroutine);
        }

        hitEffectCoroutine = StartCoroutine(HitBlinkEffect(duration));
    }

    private IEnumerator HitBlinkEffect(float duration)
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
        hitEffectCoroutine = null;
    }

    public void PlayDashEffect()
    {
        if (dashEffectPrefab == null)
        {
            Debug.Log("대시 이펙트 프리팹이 아직 없습니다.");
            return;
        }

        GameObject effect = Instantiate(
            dashEffectPrefab,
            dashEffectSpawnPoint.position,
            transform.rotation
        );

        Destroy(effect, 1f);
    }
}