using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private Hotbar hotbar;

    [Header("Slot")]
    [SerializeField] private HotbarSlotUI slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private int hotbarSlotCount = 8;

    [Header("Select")]
    [SerializeField] private RectTransform selectedFrame;

    private readonly List<HotbarSlotUI> hotbarSlots = new();
    private int selectedIndex;

    private void Start()
    {
        CreateSlots();
        Refresh();
        SelectSlot(0);
    }

    private void CreateSlots()
    {
        hotbarSlots.Clear();

        for (int i = 0; i < hotbarSlotCount; i++)
        {
            HotbarSlotUI slot = Instantiate(slotPrefab, slotParent);
            slot.Init(this, i);
            hotbarSlots.Add(slot);
        }

        if (selectedFrame != null)
            selectedFrame.SetAsLastSibling();
    }

    private void Update()
    {
        HandleNumberInput();
        HandleMouseWheel();
    }

    private void HandleNumberInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) SelectSlot(0);
        if (keyboard.digit2Key.wasPressedThisFrame) SelectSlot(1);
        if (keyboard.digit3Key.wasPressedThisFrame) SelectSlot(2);
        if (keyboard.digit4Key.wasPressedThisFrame) SelectSlot(3);
        if (keyboard.digit5Key.wasPressedThisFrame) SelectSlot(4);
        if (keyboard.digit6Key.wasPressedThisFrame) SelectSlot(5);
        if (keyboard.digit7Key.wasPressedThisFrame) SelectSlot(6);
        if (keyboard.digit8Key.wasPressedThisFrame) SelectSlot(7);
    }

    private void HandleMouseWheel()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;

        if (scroll > 0)
        {
            selectedIndex--;
            if (selectedIndex < 0)
                selectedIndex = hotbarSlots.Count - 1;

            SelectSlot(selectedIndex);
        }
        else if (scroll < 0)
        {
            selectedIndex++;
            if (selectedIndex >= hotbarSlots.Count)
                selectedIndex = 0;

            SelectSlot(selectedIndex);
        }
    }

    public void Refresh()
    {
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            ItemStack stack = hotbar.GetItem(i);
            hotbarSlots[i].SetItem(stack);
        }
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSlots.Count)
            return;

        selectedIndex = index;

        if (selectedFrame != null)
            selectedFrame.position = hotbarSlots[index].transform.position;
    }

    public ItemStack GetSelectedItem()
    {
        return hotbar.GetItem(selectedIndex);
    }
}