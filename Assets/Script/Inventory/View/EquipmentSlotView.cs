using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlotView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private ItemType equipType;
    [SerializeField] private Image itemIcon;

    private int slotIndex;
    private ItemStack equippedItem;
    private InventoryPresenter presenter;

    public ItemType EquipType => equipType;
    public ItemStack EquippedItem => equippedItem;

    public void Init(int index, InventoryPresenter inventoryPresenter)
    {
        slotIndex = index;
        presenter = inventoryPresenter;
        Clear();
    }

    public void SetItem(ItemStack item)
    {
        equippedItem = item;

        itemIcon.sprite = item.item.icon;
        itemIcon.enabled = true;
    }

    public void Clear()
    {
        equippedItem = null;

        itemIcon.sprite = null;
        itemIcon.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        presenter.OnEquipmentSlotClicked(slotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        presenter.OnEquipmentSlotHovered(slotIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        presenter.OnEquipmentSlotUnhovered(slotIndex);
    }
}