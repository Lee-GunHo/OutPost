using UnityEngine;

/// <summary>
/// 드롭 아이템의 데이터와 획득 상태를 관리
/// 충돌 감지나 GameObject 제거는 담당하지 않음.
/// </summary>
public class DroppedItemModel : MonoBehaviour
{
    [Header("Item Info")]
    [SerializeField] private ItemData itemData;

    [Min(1)]
    [SerializeField] private int amount = 1;

    private bool isPickedUp;

    public ItemData ItemData => itemData;
    public int Amount => amount;
    public bool IsPickedUp => isPickedUp;

    /// <summary>
    /// 기존 DroppedItem 컴포넌트의 직렬화 데이터를 옮길 때 사용
    /// </summary>
    public void Configure(ItemData newItemData, int newAmount)
    {
        itemData = newItemData;
        amount = Mathf.Max(1, newAmount);
    }

    public bool TryMarkPickedUp()
    {
        if (isPickedUp)
            return false;

        isPickedUp = true;
        return true;
    }

    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);
    }
}