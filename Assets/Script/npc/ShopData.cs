using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 하나의 판매 목록을 저장하는 데이터
/// 무기 상점, 식료품 상점 처럼 따로 만들 수 있음
/// </summary>
[CreateAssetMenu(fileName = "ShopData", menuName = "NPC/Shop Data")]
public class ShopData : ScriptableObject
{
    [Header("상점 판매 아이템 목록")]
    [SerializeField] private List<ShopItemData> sellItems = new List<ShopItemData>();

    public List<ShopItemData> SellItems => sellItems;
}
