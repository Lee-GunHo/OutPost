using UnityEngine;

public class InventoryPresenter : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private InventoryModel inventoryModel;
    [SerializeField] private EquipmentModel equipmentModel;

    [Header("View")]
    [SerializeField] private InventoryView inventoryView;
    [SerializeField] private EquipmentView equipmentView;

    [Header("Panel")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Presenter")]
    [SerializeField] private HotbarPresenter hotbarPresenter;

    [Header("Input")]
    [SerializeField] private UIInputManager inputManager;
    [SerializeField] private PlayerPresenter playerPresenter;
    [SerializeField] private PlayerModel playerModel;

    private bool isOpen;

    [Header("Test Items")]
    [SerializeField] private ItemData wood;
    [SerializeField] private ItemData stone;
    [SerializeField] private ItemData smileArmor;

    private ItemStack draggingItem;
    private SlotReference dragSource;

    private void Start()
    {
        inventoryView.Init(this);
        equipmentView.Init(this);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        UIState.SetInventoryOpen(false);

        inventoryModel.AddItem(wood, 50);
        inventoryModel.AddItem(stone, 25);
        inventoryModel.AddItem(smileArmor, 1);

        RefreshView();
    }

    private void OnEnable()
    {
        if (inputManager != null)
            inputManager.OnInventoryPressed += ToggleInventory;
        if (inventoryModel != null)
            inventoryModel.OnInventoryChanged += RefreshView;
    }

    private void OnDisable()
    {
        if (inputManager != null)
            inputManager.OnInventoryPressed -= ToggleInventory;
        if (inventoryModel != null)
            inventoryModel.OnInventoryChanged -= RefreshView;
    }

    private void ToggleInventory()
    {
        if (PauseMenuManager.IsMenuOpen)
            return;

        isOpen = !isOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(isOpen);

        UIState.SetInventoryOpen(isOpen);

        if (isOpen && playerPresenter != null)
            playerPresenter.StopMove();

        if (!isOpen)
        {
            StopDrag();
            inventoryView.HideTooltip();
        }
    }

    private void RefreshView()
    {
        inventoryView.Refresh(inventoryModel.Items);
        equipmentView.Refresh(equipmentModel);
    }

    public void OnInventorySlotClicked(int slotIndex)
    {
        ItemStack clickedItem = inventoryModel.Items[slotIndex];

        if (draggingItem != null)
        {
            inventoryModel.SwapItems(dragSource.SlotIndex, slotIndex);

            StopDrag();
            RefreshView();

            return;
        }

        if (clickedItem == null)
            return;

        StartDrag(
            clickedItem,
            new SlotReference(SlotType.Inventory, slotIndex)
        );
    }

    public void OnInventorySlotHovered(int slotIndex)
    {
        ItemStack item = inventoryModel.Items[slotIndex];

        if (item == null)
            return;

        inventoryView.ShowTooltip(item);
    }

    public void OnInventorySlotUnhovered(int slotIndex)
    {
        inventoryView.HideTooltip();
    }

    private void StartDrag(ItemStack item, SlotReference source)
    {
        draggingItem = item;
        dragSource = source;

        inventoryView.ShowDragIcon(item);
    }

    private void StopDrag()
    {
        draggingItem = null;
        dragSource = null;

        inventoryView.HideDragIcon();
    }
    public void OnEquipmentSlotClicked(int slotIndex)
    {
        if (draggingItem == null)
        {
            Unequip(slotIndex);
            return;
        }

        Equip(slotIndex);
    }

    public void OnEquipmentSlotHovered(int slotIndex)
    {
    }

    public void OnEquipmentSlotUnhovered(int slotIndex)
    {
    }

    private void Equip(int slotIndex)
    {
        Debug.Log("Equip 함수 호출됨");

        EquipmentSlotView slotView = equipmentView.Slots[slotIndex];

        if (draggingItem.item.itemType != slotView.EquipType)
        {
            Debug.Log("장착 불가");
            return;
        }

        ItemStack oldEquippedItem = equipmentModel.GetEquippedItem(slotView.EquipType);

        if (oldEquippedItem != null)
            playerModel.RemoveEquipmentStats(oldEquippedItem.item);

        equipmentModel.Equip(slotView.EquipType, draggingItem);
        playerModel.AddEquipmentStats(draggingItem.item);

        if (oldEquippedItem != null)
            inventoryModel.SetItemAt(dragSource.SlotIndex, oldEquippedItem);
        else
            inventoryModel.RemoveItemAt(dragSource.SlotIndex);

        StopDrag();
        RefreshView();
    }

    private void Unequip(int slotIndex)
    {
        EquipmentSlotView slotView = equipmentView.Slots[slotIndex];

        ItemStack unequippedItem = equipmentModel.Unequip(slotView.EquipType);

        if (unequippedItem == null)
            return;

        inventoryModel.AddItem(unequippedItem.item, unequippedItem.amount);

        RefreshView();
    }

    public void OnHotbarSlotClicked(int hotbarSlotIndex)
    {
        if (draggingItem == null)
            return;

        ItemStack oldHotbarItem = hotbarPresenter.GetItem(hotbarSlotIndex);

        hotbarPresenter.SetItemToSlot(hotbarSlotIndex, draggingItem);

        if (oldHotbarItem != null)
            inventoryModel.SetItemAt(dragSource.SlotIndex, oldHotbarItem);
        else
            inventoryModel.RemoveItemAt(dragSource.SlotIndex);

        StopDrag();
        RefreshView();
    }

    public void OnItemSlotClicked(SlotReference slotReference)
    {
        if (draggingItem == null)
        {
            switch (slotReference.SlotType)
            {
                case SlotType.Inventory:
                    OnInventorySlotClicked(slotReference.SlotIndex);
                    break;

                case SlotType.Hotbar:
                    ItemStack hotbarItem = hotbarPresenter.GetItem(slotReference.SlotIndex);

                    if (hotbarItem != null)
                    {
                        StartDrag(hotbarItem, slotReference);
                    }
                    break;
            }

            return;
        }

        MoveItem(dragSource, slotReference);

        StopDrag();
    
    }

    public void OnItemSlotHovered(SlotReference slotReference)
    {
        if (slotReference.SlotType == SlotType.Inventory)
            OnInventorySlotHovered(slotReference.SlotIndex);
    }

    public void OnItemSlotUnhovered(SlotReference slotReference)
    {
        if (slotReference.SlotType == SlotType.Inventory)
            OnInventorySlotUnhovered(slotReference.SlotIndex);
    }

    private void MoveItem(SlotReference from, SlotReference to)
    {
        Debug.Log($"Move : {from.SlotType}[{from.SlotIndex}] -> {to.SlotType}[{to.SlotIndex}]");

        if (from.SlotType == to.SlotType && from.SlotIndex == to.SlotIndex)
            return;

        ItemStack fromItem = GetSlotItem(from);
        ItemStack toItem = GetSlotItem(to);

        SetSlotItem(from, toItem);
        SetSlotItem(to, fromItem);

        RefreshView();
    }

    private ItemStack GetSlotItem(SlotReference slot)
    {
        switch (slot.SlotType)
        {
            case SlotType.Inventory:
                return inventoryModel.Items[slot.SlotIndex];

            case SlotType.Hotbar:
                return hotbarPresenter.GetItem(slot.SlotIndex);

            default:
                return null;
        }
    }
    private void SetSlotItem(SlotReference slot, ItemStack item)
    {
        switch (slot.SlotType)
        {
            case SlotType.Inventory:
                if (item == null)
                    inventoryModel.RemoveItemAt(slot.SlotIndex);
                else
                    inventoryModel.SetItemAt(slot.SlotIndex, item);
                break;

            case SlotType.Hotbar:
                hotbarPresenter.SetItemToSlot(slot.SlotIndex, item);
                break;
        }
    }
}