using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class InventoryUI : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private GameObject inventoryWindow;
    [SerializeField] private Inventory inventory;

    [Header("Slot Settings")]
    [SerializeField] private ItemTooltipUI tooltipUI;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private int slotCount = 40;
    [SerializeField] private List<EquipmentSlotUI> equipmentSlots;

    [Header("Test Items")]
    [SerializeField] private ItemData wood;
    [SerializeField] private ItemData stone;
    [SerializeField] private ItemData smilearmor;

    [Header("Drag")]
    [SerializeField] private Image dragIcon;

    private readonly List<InventorySlotUI> slots = new();

    private ItemStack draggingItem;
    private int draggedSlotIndex = -1;

    private ItemStack hoveredItem;

    private InventorySlotUI selectedSlot;
    private ItemStack selectedItem;

    public bool IsDragging => draggingItem != null;

    private void Start()
    {
        CreateSlots();

        foreach (var slot in equipmentSlots)
        {
            slot.Init(this);
        }

        inventory.AddItem(wood, 50);
        inventory.AddItem(stone, 25);
        inventory.AddItem(wood, 50);
        inventory.AddItem(stone, 25);
        inventory.AddItem(stone, 25);
        inventory.AddItem(stone, 25);
        inventory.AddItem(stone, 25);
        inventory.AddItem(smilearmor, 1);
        inventory.AddItem(smilearmor, 1);
        inventory.AddItem(smilearmor, 1);

        dragIcon.gameObject.SetActive(false);

        RefreshUI();
        inventoryWindow.SetActive(false);
    }

    private void Update()
    {
        if (draggingItem != null)
        {
            dragIcon.transform.position = Input.mousePosition;
        }
    }

    private void CreateSlots()
    {
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotParent);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.Init(i, this);
                slots.Add(slotUI);
            }
        }
    }

    public void RefreshUI()
    {
        foreach (var slot in slots)
        {
            slot.Clear();
        }

        for (int i = 0; i < inventory.Items.Count; i++)
        {
            if (i >= slots.Count) break;

            ItemStack stack = inventory.Items[i];

            if (stack != null)
            {
                slots[i].SetItem(stack);
            }
        }
    }

    public void StartDragItem(ItemStack item, int slotIndex)
    {
        if (item == null) return;

        draggingItem = item;
        draggedSlotIndex = slotIndex;

        selectedItem = item;

        dragIcon.sprite = item.item.icon;
        dragIcon.gameObject.SetActive(true);
        dragIcon.transform.SetAsLastSibling();
    }

    public void EndDragItem()
    {
        draggingItem = null;
        draggedSlotIndex = -1;

        dragIcon.sprite = null;
        dragIcon.gameObject.SetActive(false);
    }

    public ItemStack GetDraggingItem()
    {
        return draggingItem;
    }

    public void RemoveDraggingItemFromInventory()
    {
        if (draggedSlotIndex < 0) return;

        inventory.RemoveItemAt(draggedSlotIndex);

        selectedItem = null;
        selectedSlot = null;
    }

    public void AddItemToInventory(ItemData item, int amount)
    {
        inventory.AddItem(item, amount);
    }

    public void ClickSlot(InventorySlotUI slot)
    {
        if (!IsDragging) return;

        inventory.SwapItems(draggedSlotIndex, slot.SlotIndex);

        EndDragItem();
        RefreshUI();
    }

    public void SelectSlot(InventorySlotUI slot, ItemStack item)
    {
        selectedSlot = slot;
        selectedItem = item;

        Debug.Log("선택한 아이템: " + item.item.itemName);
    }

    public void DropSelectedItem()
    {
        if (IsDragging)
        {
            inventory.RemoveItemAt(draggedSlotIndex);
            EndDragItem();
            RefreshUI();

            Debug.Log("들고 있는 아이템을 버렸습니다.");
            return;
        }

        if (selectedItem == null || selectedSlot == null)
        {
            Debug.Log("버릴 아이템이 없습니다.");
            return;
        }

        inventory.RemoveItemAt(selectedSlot.SlotIndex);

        selectedItem = null;
        selectedSlot = null;

        RefreshUI();

        Debug.Log("아이템을 버렸습니다.");
    }

    public void SortInventory()
    {
        inventory.SortItems();

        selectedItem = null;
        selectedSlot = null;

        EndDragItem();
        RefreshUI();

        Debug.Log("인벤토리를 정렬했습니다.");
    }

    public void SetHoveredItem(ItemStack item)
    {
        hoveredItem = item;
        tooltipUI.Show(item);
    }

    public void ClearHoveredItem(ItemStack item)
    {
        if (hoveredItem == item)
        {
            hoveredItem = null;
            tooltipUI.Hide();
        }
    }
    public void OnInventory(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        inventoryWindow.SetActive(!inventoryWindow.activeSelf);
    }
}