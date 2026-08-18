using UnityEngine;
using UnityEngine.InputSystem;
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


    [Header("Hotbar Position")]
    [SerializeField] private RectTransform hotbarRect;
    [SerializeField] private Vector2 closedHotbarPosition;
    [SerializeField] private Vector2 openedHotbarPosition;


    private bool isOpen;

    public bool IsOpen => isOpen;

    [Header("Test Items")]
    [SerializeField] private bool addTestItemsOnStart;
    [SerializeField] private ItemData wood;
    [SerializeField] private ItemData stone;
    [SerializeField] private ItemData smileArmor;

    private ItemStack draggingItem;
    private SlotReference dragSource;
    private bool isSplitDrag;

    private void Start()
    {
        inventoryView.Init(this);
        equipmentView.Init(this);

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        UIState.SetInventoryOpen(false);
        UpdateHotbarPosition();

        if (addTestItemsOnStart)
        {
            AddTestItems();
        }

        RefreshView();
    }

    private void Update()
    {
        if (draggingItem == null)
            return;

        bool escapePressed =
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;

        bool rightClickPressed =
            Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame;

        if (escapePressed || rightClickPressed)
        {
            CancelDrag();
        }
    }

    private void AddTestItems()
    {
        if (wood != null)
            inventoryModel.AddItem(wood, 50);

        if (stone != null)
            inventoryModel.AddItem(stone, 25);

        if (smileArmor != null)
            inventoryModel.AddItem(smileArmor, 1);
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

        // (경민) 0709 상점 UI 닫을 때 인벤토리 UI도 같이 열렸는데 안닫혀서 수정
        if(!isOpen && UIState.IsAnyUIOpen)
        {
            Debug.Log("다른 UI가 열려 있어서 인벤토리를 열 수 없습니다.");
            return;
        }

        isOpen = !isOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(isOpen);

        UIState.SetInventoryOpen(isOpen);

        UpdateHotbarPosition();

        if (isOpen && playerPresenter != null)
            playerPresenter.StopMove();

        if (isOpen)
        {
            CraftingPresenter.Instance?.Open(null, playerPresenter, null);
        }
        else
        {
            StopDrag();
            inventoryView.HideTooltip();
            CraftingPresenter.Instance?.Close();
        }
    }

    public void SetPanelVisible(bool visible)
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(visible);
    }

    private void UpdateHotbarPosition()
    {
        if (hotbarRect == null)
            return;

        hotbarRect.anchoredPosition =
            isOpen ? openedHotbarPosition : closedHotbarPosition;
    }

    private void RefreshView()
    {
        inventoryView.Refresh(inventoryModel.Items);
        equipmentView.Refresh(equipmentModel);
    }

    private void OnInventorySlotClicked(int slotIndex)
    {
        ItemStack clickedItem = inventoryModel.GetItem(slotIndex);

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
        if (item == null || item.item == null || source == null)
            return;

        draggingItem = item;
        dragSource = source;

        // 드래그 중에는 툴팁을 숨긴다.
        inventoryView.HideTooltip();
        inventoryView.ShowDragIcon(item);
    }

    private void StopDrag()
    {
        draggingItem = null;
        dragSource = null;
        isSplitDrag = false;

        inventoryView.HideDragIcon();
    }

    private void CancelDrag()
    {
        if (isSplitDrag)
        {
            ReturnSplitItemToSource();
        }

        StopDrag();
        inventoryView.HideTooltip();
        RefreshView();
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
        if (slotIndex < 0 ||
            slotIndex >= equipmentView.Slots.Count)
        {
            return;
        }

        EquipmentSlotView slotView =
            equipmentView.Slots[slotIndex];

        ItemStack equippedItem =
            equipmentModel.GetEquippedItem(
                slotView.EquipType);

        if (equippedItem == null ||
            equippedItem.item == null)
        {
            inventoryView.HideTooltip();
            return;
        }

        inventoryView.ShowTooltip(equippedItem);
    }

    public void OnEquipmentSlotUnhovered(int slotIndex)
    {
        inventoryView.HideTooltip();
    }
    private void Equip(int slotIndex)
    {
        EquipmentSlotView slotView = equipmentView.Slots[slotIndex];

        // 드래그 중인 아이템이 없거나 데이터가 잘못된 경우
        if (draggingItem == null || draggingItem.item == null)
            return;

        // 장비 슬롯과 아이템 종류가 다르면 장착하지 않음
        if (draggingItem.item.itemType != slotView.EquipType)
        {
            Debug.Log("장착할 수 없는 아이템입니다.");
            return;
        }

        ItemStack oldEquippedItem =
            equipmentModel.GetEquippedItem(slotView.EquipType);

        // 기존 장비 능력치 제거
        if (oldEquippedItem != null)
            playerModel.RemoveEquipmentStats(oldEquippedItem.item);

        // 새로운 장비 장착 및 능력치 적용
        equipmentModel.Equip(slotView.EquipType, draggingItem);
        playerModel.AddEquipmentStats(draggingItem.item);

        // 아이템을 집었던 원래 슬롯에 기존 장비를 돌려놓음
        // 기존 장비가 없었다면 원래 슬롯이 비워짐
        SetSlotItem(dragSource, oldEquippedItem);

        StopDrag();
        RefreshView();
    }

    private void Unequip(int slotIndex)
    {
        EquipmentSlotView slotView = equipmentView.Slots[slotIndex];

        ItemStack equippedItem =
            equipmentModel.GetEquippedItem(slotView.EquipType);

        if (equippedItem == null)
            return;

        // 인벤토리에 아이템이 들어갈 공간이 있는지 먼저 확인
        if (!inventoryModel.CanAddItem(
                equippedItem.item,
                equippedItem.amount))
        {
            Debug.Log("인벤토리 공간이 부족하여 장비를 해제할 수 없습니다.");
            return;
        }

        ItemStack unequippedItem =
            equipmentModel.Unequip(slotView.EquipType);

        if (unequippedItem == null)
            return;

        // 장비로 증가했던 능력치 제거
        playerModel.RemoveEquipmentStats(unequippedItem.item);

        inventoryModel.AddItem(
            unequippedItem.item,
            unequippedItem.amount);

        RefreshView();
    }

    public void OnItemSlotSplitClicked(SlotReference slotReference)
    {
        // 이미 아이템을 드래그하고 있으면 나누지 않는다.
        if (draggingItem != null)
            return;

        ItemStack sourceItem = GetSlotItem(slotReference);

        // 1개 이하인 아이템은 나눌 수 없다.
        if (sourceItem == null ||
            sourceItem.item == null ||
            sourceItem.amount <= 1)
        {
            return;
        }

        // 홀수인 경우 원래 슬롯에 더 많은 수량을 남긴다.
        int splitAmount = sourceItem.amount / 2;

        sourceItem.amount -= splitAmount;

        ItemStack splitItem =
            new ItemStack(sourceItem.item, splitAmount);

        isSplitDrag = true;

        StartDrag(splitItem, slotReference);
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
        // 아이템을 옮기는 동안에는 툴팁을 표시하지 않는다.
        if (draggingItem != null)
        {
            inventoryView.HideTooltip();
            return;
        }

        ItemStack item = GetSlotItem(slotReference);

        if (item == null)
        {
            inventoryView.HideTooltip();
            return;
        }

        inventoryView.ShowTooltip(item);
    }

    public void OnItemSlotUnhovered(SlotReference slotReference)
    {
        inventoryView.HideTooltip();
    }
    private void MoveItem(SlotReference from, SlotReference to)
    {
        // 나누기로 집은 아이템을 같은 슬롯에 놓으면 원상 복구
        if (from.SlotType == to.SlotType &&
            from.SlotIndex == to.SlotIndex)
        {
            if (isSplitDrag)
                ReturnSplitItemToSource();

            return;
        }

        // 나누기로 집은 아이템은 별도 방식으로 배치
        if (isSplitDrag)
        {
            PlaceSplitItem(to);
            RefreshView();
            return;
        }


        ItemStack fromItem = GetSlotItem(from);
        ItemStack toItem = GetSlotItem(to);

        if (fromItem == null)
            return;

        // 같은 아이템이면 수량을 합친다.
        if (CanMergeItems(fromItem, toItem))
        {
            MergeItems(from, fromItem, toItem);
            RefreshView();
            return;
        }

        // 다른 아이템이면 서로 교환한다.
        SetSlotItem(from, toItem);
        SetSlotItem(to, fromItem);

        RefreshView();
    }
    private void PlaceSplitItem(SlotReference target)
    {
        ItemStack targetItem = GetSlotItem(target);

        // 빈 슬롯이면 분리한 아이템을 그대로 배치
        if (targetItem == null)
        {
            SetSlotItem(target, draggingItem);
            return;
        }

        // 같은 아이템이면 최대 스택까지 합친다.
        if (targetItem.item == draggingItem.item &&
            targetItem.amount < targetItem.item.maxStack)
        {
            int availableSpace =
                targetItem.item.maxStack - targetItem.amount;

            int moveAmount =
                Mathf.Min(availableSpace, draggingItem.amount);

            targetItem.amount += moveAmount;
            draggingItem.amount -= moveAmount;
        }

        // 들어가지 못한 나머지는 원래 슬롯으로 반환
        if (draggingItem.amount > 0)
        {
            ReturnSplitItemToSource();
        }
    }
    private void ReturnSplitItemToSource()
    {
        if (draggingItem == null || dragSource == null)
            return;

        ItemStack sourceItem = GetSlotItem(dragSource);

        if (sourceItem != null &&
            sourceItem.item == draggingItem.item)
        {
            sourceItem.amount += draggingItem.amount;
        }
        else
        {
            SetSlotItem(dragSource, draggingItem);
        }

        draggingItem.amount = 0;
    }
    private bool CanMergeItems(ItemStack fromItem, ItemStack toItem)
    {
        if (fromItem == null || toItem == null)
            return false;

        if (fromItem.item == null || toItem.item == null)
            return false;

        if (fromItem.item != toItem.item)
            return false;

        return toItem.amount < toItem.item.maxStack;
    }
    private void MergeItems(
    SlotReference from,
    ItemStack fromItem,
    ItemStack toItem)
    {
        int availableSpace =
            toItem.item.maxStack - toItem.amount;

        int moveAmount =
            Mathf.Min(availableSpace, fromItem.amount);

        toItem.amount += moveAmount;
        fromItem.amount -= moveAmount;

        // 원래 아이템을 전부 옮겼으면 원래 슬롯을 비운다.
        if (fromItem.amount <= 0)
        {
            SetSlotItem(from, null);
        }
        else
        {
            // 최대 수량을 초과한 아이템은 원래 슬롯에 남긴다.
            SetSlotItem(from, fromItem);
        }
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
    public void OnTrashButtonClicked()
    {
        if (draggingItem == null || dragSource == null)
            return;

        if (!isSplitDrag)
        {
            // 일반 드래그는 원래 슬롯에 아이템이 그대로 있으므로
            // 원래 슬롯을 비워 전체 스택을 삭제한다.
            SetSlotItem(dragSource, null);
        }

        // 분리 드래그는 집은 수량이 이미 원래 스택에서 빠져 있으므로
        // 원래 슬롯을 건드리지 않고 드래그 중인 수량만 삭제한다.

        StopDrag();
        inventoryView.HideTooltip();
        RefreshView();

        Debug.Log("아이템을 버렸습니다.");
    }
    public void OnSortButtonClicked()
    {
        // 아이템을 들고 있는 동안에는 정렬하지 않는다.
        if (draggingItem != null)
        {
            Debug.Log("아이템을 이동 중에는 정렬할 수 없습니다.");
            return;
        }

        inventoryModel.SortItems();
        RefreshView();

        Debug.Log("인벤토리를 정렬했습니다.");
    }
    public void RefreshAfterLoad()
    {
        RefreshView();
    }
}