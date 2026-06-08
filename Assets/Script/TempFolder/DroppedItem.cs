using UnityEngine;

/// <summary>
/// 바닥에 떨어진 아이템에 붙임
/// 플레이어가 아이템에 닿으면 Inventory에 아이템을 추가하고
/// 바닥의 아이템 오브젝트는 사라짐
/// </summary>
public class DroppedItem : MonoBehaviour
{
    [Header("Item Info")]
    [Tooltip("아이템 이름")]
    public string itemName = "Stone";

    [Tooltip("한 번 먹었을 때 몇 개 획득할래?")]
    public int amount = 1;

    /// <summary>
    /// Trigger Collider에 다른 Collider가 들어왔을 때 자동으로 호출
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        // 닿은 오브젝트에게서 Inventory 컴포넌트 찾기
        TempInventory inventory = other.GetComponent<TempInventory>();

        // 부모 오브젝트에 있을 수도 있으므로 부모에서 한 번 더 찾기
        if(inventory == null)
        {
            inventory = other.GetComponentInParent<TempInventory>();
        }

        // Inventory가 없다면 플레이어가 아닌 다른 물체에 닿은 것이니까 아무것도 하지 않기
        if (inventory == null)
            return;

        // 인벤토리에 아이템 추가
        inventory.AddItem(itemName, amount);

        // 아이템 먹었으니까 씬에서 제거
        Destroy(gameObject);
    }
}
