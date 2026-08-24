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

    public ItemData ItemData => itemData;
    public int BuyPrice => buyPrice;
    public int SellPrice => sellPrice;
}
