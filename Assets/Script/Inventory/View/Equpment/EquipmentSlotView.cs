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
    private InventoryPresenter presenter;

    public ItemType EquipType => equipType;

    public void Init(int index, InventoryPresenter inventoryPresenter)
    {
        slotIndex = index;
        presenter = inventoryPresenter;
        Clear();
    }

    public void SetItem(ItemStack itemStack)
    {
        if (itemStack == null || itemStack.item == null)
        {
            itemIcon.enabled = false;
            itemIcon.sprite = null;
            return;
        }

        itemIcon.enabled = true;
        itemIcon.sprite = itemStack.item.icon;
    }

    public void Clear()
    {
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