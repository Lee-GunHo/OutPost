using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [Header("Renderer")]
    [Tooltip("실제 보이는 FBX 오브젝트를 넣으세요. 비워두면 자식 Renderer 중 현재 켜져 있는 것만 자동으로 잡습니다.")]
    [SerializeField] private Transform visualRoot;

    [Header("Dash Effect")]
    [SerializeField] private GameObject dashEffectPrefab;
    [SerializeField] private Transform dashEffectSpawnPoint;

    private readonly List<Renderer> hitBlinkRenderers = new List<Renderer>();
    private readonly List<bool> originalRendererEnabledStates = new List<bool>();

    private Coroutine hitEffectCoroutine;

    [Header("Roll (Dash)")]
    [Tooltip("대시할 때 앞으로 한 바퀴 구르는 모션")]
    [SerializeField] private bool rollOnDash = true;
    [Tooltip("구르는 동안 몸을 웅크리는 정도(0~1)")]
    [Range(0f, 0.6f)] [SerializeField] private float rollTuck = 0.25f;

    private bool isRolling;
    private float rollElapsed;
    private float rollDuration;
    private Vector3 rollAxis;
    private float rollPivotHeight;
    private Vector3 rollRestLocalPosition;
    private Quaternion rollRestLocalRotation;
    private Vector3 rollRestLocalScale;

    [Header("Walking Animation")]
    [SerializeField] private Animator walkingAnimator;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private bool isWalking;

    private void Awake()
    {
        CacheRenderers();

        if (walkingAnimator == null && visualRoot != null)
            walkingAnimator = visualRoot.GetComponentInChildren<Animator>(true);

        if (dashEffectSpawnPoint == null)
        {
            dashEffectSpawnPoint = transform;
        }
    }

    public void SetWalking(bool walking)
    {
        if (walkingAnimator == null || walkingAnimator.runtimeAnimatorController == null)
            return;
        if (isWalking == walking)
            return;

        isWalking = walking;
        walkingAnimator.SetBool(IsWalkingHash, walking);
    }

    private void OnDisable()
    {
        SetWalking(false);
        StopRoll();
    }

    /// <summary>대시 방향으로 한 바퀴 구름. 애니메이션 클립 없이 보이는 모델(visualRoot)을 몸 중심 기준으로 회전</summary>
    public void PlayRoll(Vector3 direction, float duration)
    {
        if (!rollOnDash || visualRoot == null || visualRoot == transform || visualRoot.parent == null)
            return;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            direction = transform.forward;

        StopRoll();

        rollRestLocalPosition = visualRoot.localPosition;
        rollRestLocalRotation = visualRoot.localRotation;
        rollRestLocalScale = visualRoot.localScale;
        // 앞구르기: 머리가 진행 방향으로 넘어가도록 진행 방향의 오른쪽 축으로 회전
        rollAxis = Vector3.Cross(Vector3.up, direction.normalized);
        rollPivotHeight = GetVisualCenterHeight();
        rollDuration = Mathf.Max(0.1f, duration);
        rollElapsed = 0f;
        isRolling = true;
    }

    // 애니메이터가 갱신한 뒤에 덮어써야 해서 LateUpdate에서 적용
    private void LateUpdate()
    {
        if (!isRolling)
            return;

        rollElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(rollElapsed / rollDuration);

        if (t >= 1f)
        {
            StopRoll();
            return;
        }

        float eased = t * t * (3f - 2f * t);
        Quaternion spin = Quaternion.AngleAxis(360f * eased, rollAxis);
        float scale = 1f - Mathf.Sin(t * Mathf.PI) * rollTuck;

        Transform parent = visualRoot.parent;
        Vector3 restPosition = parent.TransformPoint(rollRestLocalPosition);
        Quaternion restRotation = parent.rotation * rollRestLocalRotation;
        Vector3 pivot = transform.position + Vector3.up * rollPivotHeight;

        visualRoot.position = pivot + spin * (restPosition - pivot) * scale;
        visualRoot.rotation = spin * restRotation;
        visualRoot.localScale = rollRestLocalScale * scale;
    }

    private void StopRoll()
    {
        if (!isRolling)
            return;

        isRolling = false;
        visualRoot.localPosition = rollRestLocalPosition;
        visualRoot.localRotation = rollRestLocalRotation;
        visualRoot.localScale = rollRestLocalScale;
    }

    // 보이는 모델의 높이 중심(발밑 기준). 렌더러가 없으면 대략적인 사람 키의 절반
    private float GetVisualCenterHeight()
    {
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (Renderer visualRenderer in visualRoot.GetComponentsInChildren<Renderer>())
        {
            if (!visualRenderer.enabled || visualRenderer.name.StartsWith("__"))
                continue;

            if (!hasBounds)
            {
                bounds = visualRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(visualRenderer.bounds);
            }
        }

        return hasBounds ? Mathf.Max(0.2f, bounds.center.y - transform.position.y) : 0.9f;
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

            // Player 루트 오브젝트에 붙은 숨김용 Renderer는 제외
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

    public void PlayHitBlinkEffect(float duration)
    {
        if (hitBlinkRenderers.Count == 0)
        {
            CacheRenderers();
        }

        if (hitBlinkRenderers.Count == 0)
        {
            Debug.LogWarning("PlayerView: 깜빡임에 사용할 Renderer가 없습니다.");
            return;
        }

        if (hitEffectCoroutine != null)
        {
            StopCoroutine(hitEffectCoroutine);
            RestoreOriginalRendererStates();
        }

        hitEffectCoroutine = StartCoroutine(HitBlinkEffect(duration));
    }

    private IEnumerator HitBlinkEffect(float duration)
    {
        float timer = 0f;
        float blinkInterval = 0.08f;

        while (timer < duration)
        {
            SetRenderersVisible(false);
            yield return new WaitForSeconds(blinkInterval);

            SetRenderersVisible(true);
            yield return new WaitForSeconds(blinkInterval);

            timer += blinkInterval * 2f;
        }

        RestoreOriginalRendererStates();
        hitEffectCoroutine = null;
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

    public void PlayDashEffect()
    {
        SfxPlayer.PlayAt(SfxId.Dash, transform.position);

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
