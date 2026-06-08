using System.Collections.Generic;
using UnityEngine;

// 아주 간단한 인벤토리 스크립트입니다.
// 아이템 이름과 개수를 Dictionary로 저장합니다.
//
// 예:
// "Stone" → 3개
// "Wood" → 5개
public class TempInventory : MonoBehaviour
{
    // Dictionary는 "이름표 + 값" 형태로 데이터를 저장하는 자료구조
    // 여기서는 itemName을 Key로, 개수를 Value로 저장
    private Dictionary<string, int> items = new Dictionary<string, int>();

    // 아이템을 인벤토리에 추가하는 함수
    // DroppedItem.cs에서 호출
    public void AddItem(string itemName, int amount)
    {
        // 이미 같은 이름의 아이템이 있으면 개수만 증가
        if (items.ContainsKey(itemName))
        {
            items[itemName] += amount;
        }
        // 처음 먹는 아이템이면 새로 등록
        else
        {
            items.Add(itemName, amount);
        }

        // 현재 획득 결과를 Console에 출력
        Debug.Log(itemName + " 획득! 현재 개수: " + items[itemName]);
    }

    // 특정 아이템이 몇 개 있는지 확인하는 함수
    public int GetItemCount(string itemName)
    {
        // 해당 아이템이 있으면 개수를 반환
        if (items.ContainsKey(itemName))
        {
            return items[itemName];
        }

        // 없으면 0개입니다.
        return 0;
    }

    // 현재 인벤토리 전체 내용을 Console에 출력하는 함수
    // 테스트용
    public void PrintInventory()
    {
        Debug.Log("===== Inventory =====");

        foreach (KeyValuePair<string, int> item in items)
        {
            Debug.Log(item.Key + " : " + item.Value);
        }
    }
}