using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarView : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private ItemSlotView slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private int hotbarSlotCount = 8;

    [Header("Select")]
    [SerializeField] private RectTransform selectedFrame;

    private readonly List<ItemSlotView> hotbarSlots = new();

    public int SlotCount => hotbarSlotCount;

    public event Action<int> OnNumberKeyPressed;
    public event Action<SlotReference> OnSlotClicked;
    public event Action<SlotReference> OnSlotSplitClicked;

    public event Action<SlotReference> OnSlotHovered;
    public event Action<SlotReference> OnSlotUnhovered;

    public event Action OnMouseWheelUp;
    public event Action OnMouseWheelDown;

    private void Update()
    {
        HandleNumberInput();
        HandleMouseWheelInput();
    }

    public void CreateSlots()
    {
        hotbarSlots.Clear();

        for (int i = 0; i < hotbarSlotCount; i++)
        {
            ItemSlotView slot = Instantiate(slotPrefab, slotParent);

            slot.Initialize(SlotType.Hotbar, i);
            slot.OnSlotClicked += HandleSlotClicked;
            slot.OnSlotSplitClicked += HandleSlotSplitClicked;
            slot.OnSlotHovered += HandleSlotHovered;
            slot.OnSlotUnhovered += HandleSlotUnhovered;
            hotbarSlots.Add(slot);
        }

        if (selectedFrame != null)
        {
            selectedFrame.SetAsLastSibling();
            MoveSelectedFrame(0);
        }
    }

    private void HandleSlotHovered(SlotReference slotReference)
    {
        OnSlotHovered?.Invoke(slotReference);
    }

    private void HandleSlotUnhovered(SlotReference slotReference)
    {
        OnSlotUnhovered?.Invoke(slotReference);
    }

    public void Refresh(List<ItemStack> items)
    {
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            hotbarSlots[i].SetItem(items[i]);
        }
    }

    public void MoveSelectedFrame(int index)
    {
        if (selectedFrame == null) return;
        if (index < 0 || index >= hotbarSlots.Count) return;

        RectTransform slotRect = hotbarSlots[index].GetComponent<RectTransform>();

        selectedFrame.SetParent(slotRect, false);
        selectedFrame.SetAsLastSibling();

        selectedFrame.anchorMin = Vector2.zero;
        selectedFrame.anchorMax = Vector2.one;
        selectedFrame.pivot = new Vector2(0.5f, 0.5f);

        selectedFrame.offsetMin = Vector2.zero;
        selectedFrame.offsetMax = Vector2.zero;
    }

    private void HandleSlotClicked(SlotReference slotReference)
    {
        OnSlotClicked?.Invoke(slotReference);
    }
    private void HandleSlotSplitClicked(SlotReference slotReference)
    {
        OnSlotSplitClicked?.Invoke(slotReference);
    }
    private void HandleNumberInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) OnNumberKeyPressed?.Invoke(0);
        if (keyboard.digit2Key.wasPressedThisFrame) OnNumberKeyPressed?.Invoke(1);
        if (keyboard.digit3Key.wasPressedThisFrame) OnNumberKeyPressed?.Invoke(2);
        if (keyboard.digit4Key.wasPressedThisFrame) OnNumberKeyPressed?.Invoke(3);
        if (keyboard.digit5Key.wasPressedThisFrame) OnNumberKeyPressed?.Invoke(4);
        if (keyboard.digit6Key.wasPressedThisFrame) OnNumberKeyPressed?.Invoke(5);
        if (keyboard.digit7Key.wasPressedThisFrame) OnNumberKeyPressed?.Invoke(6);
        if (keyboard.digit8Key.wasPressedThisFrame) OnNumberKeyPressed?.Invoke(7);
    }

    private void HandleMouseWheelInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        float scrollValue = mouse.scroll.ReadValue().y;

        if (scrollValue > 0)
            OnMouseWheelUp?.Invoke();
        else if (scrollValue < 0)
            OnMouseWheelDown?.Invoke();
    }
}