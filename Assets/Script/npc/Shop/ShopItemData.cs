using UnityEngine;

/// <summary>
/// 상점 아이템 하나의 정보 저장
/// </summary>
[System.Serializable]
public class ShopItemData
{
    [Header("아이템 정보")]
    [SerializeField] private ItemData itemData;

    [Header("가격 정보")]
    // 플레이어가 이 아이템을 구매할 때 필요한 가격
    [SerializeField] private int buyPrice;
    // 플레이어가 이 아이템을 판매할 때 받는 가격
    [SerializeField] private int sellPrice;

    [Header("지급 한도")]
    [Tooltip("이 아이템을 상점에서 받을 수 있는 총 개수(저장됨). 0이면 무제한")]
    [Min(0)]
    [SerializeField] private int maxTotalGive;

    public ItemData ItemData => itemData;
    public int MaxTotalGive => maxTotalGive;
    public int BuyPrice => buyPrice;
    public int SellPrice => sellPrice;
}
