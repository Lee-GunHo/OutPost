using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 상점 하나의 교환 목록을 저장하는 데이터
/// 갑옷 상점, 자원 상점 처럼 따로 만들 수 있음
/// </summary>
[CreateAssetMenu(fileName = "ShopData", menuName = "NPC/Shop Data")]
public class ShopData : ScriptableObject
{
    [Header("교환 목록 (재료를 내고 받는 아이템)")]
    [FormerlySerializedAs("shopSellItems")]
    [SerializeField] private List<ShopItemData> tradeItems = new List<ShopItemData>();

    public List<ShopItemData> TradeItems => tradeItems;
}
