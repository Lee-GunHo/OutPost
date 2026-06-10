using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class HotbarSlotUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;

    private HotbarUI hotbarUI;
    private int slotIndex;

    public void Init(HotbarUI ui, int index)
    {
        hotbarUI = ui;
        slotIndex = index;
        Clear();
    }

    public void SetItem(ItemStack stack)
    {
        if (stack == null || stack.item == null)
        {
            Clear();
            return;
        }

        itemIcon.sprite = stack.item.icon;
        itemIcon.enabled = true;

        countText.text = stack.amount > 1 ? stack.amount.ToString() : "";
    }

    public void Clear()
    {
        itemIcon.sprite = null;
        itemIcon.enabled = false;
        countText.text = "";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        hotbarUI.SelectSlot(slotIndex);
    }
}