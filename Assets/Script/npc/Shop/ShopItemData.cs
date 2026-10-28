using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물물교환에 필요한 재료 하나(아이템 + 개수)
/// </summary>
[System.Serializable]
public class ShopCost
{
    [SerializeField] private ItemData item;
    [Min(1)]
    [SerializeField] private int amount = 1;

    public ShopCost() { }

    public ShopCost(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }

    public ItemData Item => item;
    public int Amount => amount;
}

/// <summary>
/// 상점 교환 항목 하나의 정보 저장 (돈 대신 몬스터 드랍 재료로 교환)
/// </summary>
[System.Serializable]
public class ShopItemData
{
    [Header("받는 아이템")]
    [SerializeField] private ItemData itemData;

    [Tooltip("교환 1회에 받는 개수")]
    [Min(1)]
    [SerializeField] private int giveAmount = 1;

    [Header("교환 재료 (교환 1회 기준)")]
    [Tooltip("예: Small Mushroom 3, Spore 1. 비어 있으면 무료")]
    [SerializeField] private List<ShopCost> costs = new List<ShopCost>();

    [Header("지급 한도")]
    [Tooltip("이 아이템을 상점에서 받을 수 있는 총 개수(저장됨). 0이면 무제한")]
    [Min(0)]
    [SerializeField] private int maxTotalGive;

    public ItemData ItemData => itemData;
    public int GiveAmount => Mathf.Max(1, giveAmount);
    public List<ShopCost> Costs => costs;
    public int MaxTotalGive => maxTotalGive;
}
