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
/// 창고 UI에서 사용하는 일반 슬롯 View
/// 우클릭 드래그 시작과 슬롯 클릭 입력만 Presenter에 전달
/// </summary>
public class ChestSlotView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text countText;

    [Header("슬롯 배경")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Sprite defaultSlotSprite;
    [SerializeField] private Sprite selectedSlotSprite;

    private ChestSlotArea slotArea;
    private int slotIndex;
    private ItemStack currentItem;

    private bool isRightDragging;
    private int lastDragEndFrame = -1;

    public ChestSlotArea SlotArea => slotArea;
    public int SlotIndex => slotIndex;
    public ItemStack CurrentItem => currentItem;

    public event Action<
        ChestSlotArea,
        int,
        PointerEventData.InputButton,
        bool
    > OnSlotClicked;

    public event Action<ChestSlotArea, int> OnSlotHovered;
    public event Action OnSlotUnhovered;
    public event Action<ChestSlotArea, int> OnRightDragStarted;

    public void Initialize(ChestSlotArea area, int index)
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
            itemIcon.enabled = itemStack.item.icon != null;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        // 우클릭 드래그를 끝낸 프레임에 우클릭 이벤트까지 발생하는 것을 막음
        if (Time.frameCount == lastDragEndFrame)
            return;

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isRightDragging)
            return;

        OnSlotHovered?.Invoke(slotArea, slotIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnSlotUnhovered?.Invoke();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 이 기능은 우클릭 드래그로만 시작
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (IsEmpty(currentItem))
            return;

        isRightDragging = true;
        OnSlotUnhovered?.Invoke();
        OnRightDragStarted?.Invoke(slotArea, slotIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 커서 아이콘 위치는 ChestView.Update에서 처리
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isRightDragging)
            return;

        isRightDragging = false;
        lastDragEndFrame = Time.frameCount;

        // 드래그가 끝나도 Presenter가 들고 있는 아이템은 유지됨
        // 이후 좌클릭할 때마다 1개씩 놓음
    }

    private static bool IsEmpty(ItemStack itemStack)
    {
        return itemStack == null ||
               itemStack.item == null ||
               itemStack.amount <= 0;
    }
}