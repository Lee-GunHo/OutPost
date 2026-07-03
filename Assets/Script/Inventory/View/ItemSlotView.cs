using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemSlotView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;

    private SlotReference slotReference;
    private ItemStack currentItem;

    public SlotReference SlotReference => slotReference;
    public ItemStack CurrentItem => currentItem;

    public event Action<SlotReference> OnSlotClicked;
    public event Action<SlotReference> OnSlotHovered;
    public event Action<SlotReference> OnSlotUnhovered;

    public void Initialize(SlotType slotType, int slotIndex)
    {
        slotReference = new SlotReference(slotType, slotIndex);
        Clear();
    }

    public void SetItem(ItemStack itemStack)
    {
        currentItem = itemStack;

        if (itemStack == null || itemStack.item == null)
        {
            Clear();
            return;
        }

        itemIcon.sprite = itemStack.item.icon;
        itemIcon.enabled = itemStack.item.icon != null;

        if (countText != null)
            countText.text = itemStack.amount > 1 ? itemStack.amount.ToString() : "";
    }

    /// <summary>
    /// (경민) 상점 슬롯에 ItemData를 직접 넣기 위해 만든 함수
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="amount"></param>
    public void SetItem(ItemData itemData, int amount)
    {
        if(itemData == null)
        {
            Clear();
            return;
        }

        itemIcon.sprite = itemData.icon;
        itemIcon.enabled = itemData.icon != null;

        if(countText != null)
        {
            countText.text = amount > 1 ? amount.ToString() : "";
        }
    }

    public void Clear()
    {
        currentItem = null;

        itemIcon.sprite = null;
        itemIcon.enabled = false;

        if (countText != null)
            countText.text = "";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(slotReference);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnSlotHovered?.Invoke(slotReference);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnSlotUnhovered?.Invoke(slotReference);
    }
}