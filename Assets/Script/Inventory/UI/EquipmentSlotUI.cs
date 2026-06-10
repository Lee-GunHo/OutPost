using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private ItemType equipType;
    [SerializeField] private Image itemIcon;

    private ItemStack equippedItem;
    private InventoryUI inventoryUI;

    public void Init(InventoryUI ui)
    {
        inventoryUI = ui;
        Clear();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!inventoryUI.IsDragging)
        {
            if (equippedItem != null)
            {
                inventoryUI.AddItemToInventory(
                    equippedItem.item,
                    equippedItem.amount
                );

                Clear();
                inventoryUI.RefreshUI();

                Debug.Log("장비 해제");
            }

            return;
        }

        ItemStack draggingItem = inventoryUI.GetDraggingItem();

        if (draggingItem == null)
            return;

        if (draggingItem.item.itemType != equipType)
        {
            Debug.Log("장착 불가");
            return;
        }

        if (equippedItem != null)
        {
            inventoryUI.AddItemToInventory(
                equippedItem.item,
                equippedItem.amount
            );
        }

        Equip(draggingItem);

        inventoryUI.RemoveDraggingItemFromInventory();
        inventoryUI.EndDragItem();
        inventoryUI.RefreshUI();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (equippedItem == null)
            return;

        inventoryUI.SetHoveredItem(equippedItem);
    }
    private void Equip(ItemStack item)
    {
        equippedItem = item;

        itemIcon.sprite = item.item.icon;
        itemIcon.enabled = true;

        Debug.Log("장착: " + item.item.itemName);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (equippedItem == null)
            return;

        inventoryUI.ClearHoveredItem(equippedItem);
    }
    private void Clear()
    {
        equippedItem = null;

        itemIcon.sprite = null;
        itemIcon.enabled = false;
    }
}