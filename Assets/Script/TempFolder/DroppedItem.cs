using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    [Header("Item Info")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    private bool isPickedUp = false;
    private InventoryModel inventoryModel;

    private void Awake()
    {
        inventoryModel = FindFirstObjectByType<InventoryModel>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("드랍아이템 충돌 감지 : " + other.name);

        if (isPickedUp)
            return;

        if (!other.CompareTag("Player"))
        {
            Debug.Log("Player 태그가 아니라서 return됨 : " + other.tag);
            return;
        }

        if (inventoryModel == null)
        {
            Debug.LogError("InventoryModel 못 찾음");
            return;
        }

        if (itemData == null)
        {
            Debug.LogWarning("ItemData 비어있음");
            return;
        }

        Debug.Log("AddItem 실행 직전 : " + itemData.name + " / " + amount);

        bool success = inventoryModel.AddItem(itemData, amount);

        Debug.Log("AddItem 결과 : " + success);

        if (!success)
            return;

        isPickedUp = true;
        Destroy(gameObject);
    }
}