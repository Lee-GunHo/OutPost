using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;

    private int slotIndex;
    private ItemStack currentItem;
    private InventoryPresenter presenter;

    public int SlotIndex => slotIndex;
    public ItemStack CurrentItem => currentItem;

    public void Init(int index, InventoryPresenter inventoryPresenter)
    {
        slotIndex = index;
        presenter = inventoryPresenter;
        Clear();
    }

    public void SetItem(ItemStack item)
    {
        currentItem = item;

        itemIcon.sprite = item.item.icon;
        itemIcon.enabled = true;

        countText.text = item.amount > 1 ? item.amount.ToString() : "";
    }

    public void Clear()
    {
        currentItem = null;

        itemIcon.sprite = null;
        itemIcon.enabled = false;

        countText.text = "";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        presenter.OnInventorySlotClicked(slotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        presenter.OnInventorySlotHovered(slotIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        presenter.OnInventorySlotUnhovered(slotIndex);
    }
}