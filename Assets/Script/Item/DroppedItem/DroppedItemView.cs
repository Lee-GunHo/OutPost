using System;
using UnityEngine;

/// <summary>
/// 드롭 아이템의 Unity 이벤트와 시각적 제거를 담당
/// 획득 가능 여부나 인벤토리 처리는 판단하지 않음.
/// </summary>
public class DroppedItemView : MonoBehaviour
{
    [Header("Icon Visual")]
    [Tooltip("설정하면 ItemData.icon을 이 SpriteRenderer에 표시하고 카메라를 향해 빌보드 처리합니다. 3D 모델 드랍템은 비워두세요.")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private bool billboardToCamera = true;

    public event Action<Collider> TriggerEntered;

    private void OnTriggerEnter(Collider other)
    {
        TriggerEntered?.Invoke(other);
    }

    public void SetIcon(Sprite icon)
    {
        if (iconRenderer != null)
            iconRenderer.sprite = icon;
    }

    private void LateUpdate()
    {
        if (!billboardToCamera || iconRenderer == null)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        iconRenderer.transform.forward = cam.transform.forward;
    }

    public void DestroyItem()
    {
        Destroy(gameObject);
    }
}