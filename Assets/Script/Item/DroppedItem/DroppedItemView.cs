using System;
using UnityEngine;

/// <summary>
/// 드롭 아이템의 Unity 이벤트와 시각적 제거를 담당
/// 획득 가능 여부나 인벤토리 처리는 판단하지 않음.
/// </summary>
public class DroppedItemView : MonoBehaviour
{
    public event Action<Collider> TriggerEntered;

    private void OnTriggerEnter(Collider other)
    {
        TriggerEntered?.Invoke(other);
    }

    public void DestroyItem()
    {
        Destroy(gameObject);
    }
}