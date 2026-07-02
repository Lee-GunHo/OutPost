/*using UnityEngine;

/// <summary>
/// 바닥에 떨어진 아이템에 붙이는 스크립트
/// 플레이어가 아이템에 닿으면 InventoryModel에 아이템을 추가하고
/// 추가하면 바닥의 아이템 오브젝트는 사라짐
/// </summary>
public class DroppedItem : MonoBehaviour
{
    [Header("Item Info")]

    [Tooltip("획득할 아이템 데이터")]
    [SerializeField] private ItemData itemData;

    [Tooltip("바닥에 떨어져 있는 아이템 개수")]
    [SerializeField] private int amount = 1;

    // 중복 획득 방지용 변수
    private bool isPickedUp = false;

    /// <summary>
    /// Trigger Collider에 다른 Collider가 들어왔을 때 자동으로 호출
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        // 이미 획득 처리 중이면 다시 실행하지 않음
        if (isPickedUp)
            return;

        // 닿은 오브젝트에게서 Inventory 컴포넌트 찾기
        InventoryModel inventory = other.GetComponent<InventoryModel>();

        // InventoryModel이 없으면 플레이어가 아닌 물체에 닿은 것이므로 무시
        if(inventory == null)
            return;

        // 아이템 데이터가 비어있으면 추가할 수 없으니까 경고 출력하고 종료
        if(itemData == null)
        {
            Debug.LogWarning($"{gameObject.name}에 ItemData가 연결되어 있지 않습니다");
            return;
        }

        // 인벤토리에 아이템 추가
        bool success = inventory.AddItem(itemData, amount);

        // 인벤토리가 가득 차서 추가하지 못했다면 바닥에 있는 아이템 없애지 않음
        if (!success)
            return;

        // 정상적으로 획득
        isPickedUp = true;

        // 바닥에 떨어진 아이템 제거
        Destroy(gameObject);
    }
}
*/

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