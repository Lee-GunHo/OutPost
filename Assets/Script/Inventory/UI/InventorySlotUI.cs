using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler


{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;

    private ItemStack currentItem;
    private InventoryUI inventoryUI;

    public int SlotIndex { get; private set; }

    public void Init(int index, InventoryUI ui)
    {
        SlotIndex = index;
        inventoryUI = ui;
        Clear();
    }

    public void SetItem(ItemStack stack)
    {
        currentItem = stack;

        itemIcon.sprite = stack.item.icon;
        itemIcon.enabled = true;

        countText.text =
            stack.amount > 1
            ? stack.amount.ToString()
            : "";
    }

    public void Clear()
    {
        currentItem = null;

        itemIcon.sprite = null;
        itemIcon.enabled = false;

        countText.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null)
            inventoryUI.SetHoveredItem(currentItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (inventoryUI != null)
            inventoryUI.ClearHoveredItem(currentItem);
    }



    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("슬롯 클릭됨");

    if (currentItem == null)
{
    if (inventoryUI.IsDragging)
    {
        inventoryUI.ClickSlot(this);
    }

    return;
}

        if (inventoryUI.IsDragging)
        {
            inventoryUI.ClickSlot(this);
            return;
        }
        else
        {
            inventoryUI.SelectSlot(this, currentItem);
            inventoryUI.StartDragItem(currentItem, SlotIndex);
        }
    }
}