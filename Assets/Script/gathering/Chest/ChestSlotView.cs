using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ChestSlotArea
{
    Hotbar,
    Inventory,
    Chest
}

/// <summary>
/// 창고 UI에서 사용하는 슬롯 하나의 View
/// </summary>
public class ChestSlotView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text countText;

    private ChestSlotArea slotArea;
    private int slotIndex;
    private ItemStack currentItem;

    public ChestSlotArea SlotArea => slotArea;
    public int SlotIndex => slotIndex;
    public ItemStack CurrentItem => currentItem;

    public event Action<
        ChestSlotArea,
        int,
        PointerEventData.InputButton,
        bool
    > OnSlotClicked;

    public event Action<ChestSlotArea, int>
        OnSlotHovered;

    public event Action OnSlotUnhovered;

    public void Initialize(
        ChestSlotArea area,
        int index)
    {
        slotArea = area;
        slotIndex = index;

        Clear();
    }

    public void SetItem(ItemStack itemStack)
    {
        currentItem = itemStack;

        if (IsEmpty(itemStack))
        {
            Clear();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = itemStack.item.icon;
            itemIcon.enabled =
                itemStack.item.icon != null;
        }

        if (countText != null)
        {
            countText.text = itemStack.amount > 1
                ? itemStack.amount.ToString()
                : string.Empty;
        }
    }

    public void Clear()
    {
        currentItem = null;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (countText != null)
        {
            countText.text = string.Empty;
        }
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        bool isShiftPressed =
            Keyboard.current != null &&
            (
                Keyboard.current.leftShiftKey.isPressed ||
                Keyboard.current.rightShiftKey.isPressed
            );

        OnSlotClicked?.Invoke(
            slotArea,
            slotIndex,
            eventData.button,
            isShiftPressed
        );
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        OnSlotHovered?.Invoke(
            slotArea,
            slotIndex
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        OnSlotUnhovered?.Invoke();
    }

    private static bool IsEmpty(
        ItemStack itemStack)
    {
        return itemStack == null ||
               itemStack.item == null ||
               itemStack.amount <= 0;
    }
}