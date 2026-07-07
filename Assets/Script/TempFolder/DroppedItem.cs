using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    [Header("Item Info")]
    [SerializeField] private ItemData itemData;

    [SerializeField] private int amount = 1;

    private bool isPickedUp;

    private void OnTriggerEnter(Collider other)
    {
        if (isPickedUp)
            return;

        PlayerPresenter player = other.GetComponentInParent<PlayerPresenter>();

        if (player == null)
            return;

        InventoryModel inventory = player.PlayerInventory;

        if(inventory == null)
        {
            Debug.LogWarning("플레이어에게 InventoryModel이 연결되어 있지 않습니다.");
            return;
        }

        if(itemData == null)
        {
            Debug.LogWarning(gameObject.name + "에 ItemData가 연결되어 있지 않습니다.");
            return;
        }

        bool success = inventory.AddItem(itemData, amount);

        if(!success)
        {
            Debug.Log("인벤토리가 가득 차서 아이템을 획득하지 못했습니다.");
            return;
        }

        isPickedUp = true;
        Destroy(gameObject);
    }
}