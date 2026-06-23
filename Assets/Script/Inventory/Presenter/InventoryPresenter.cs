using System.Collections.Generic;
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

    [Header("Input")]
    [SerializeField] private UIInputManager inputManager;
    [SerializeField] private PlayerPresenter playerPresenter;

    private bool isOpen;


    [Header("Test Items")]
    [SerializeField] private ItemData wood;
    [SerializeField] private ItemData stone;
    [SerializeField] private ItemData smileArmor;


    private ItemStack draggingItem;
    private int draggedSlotIndex = -1;

    private void Start()
    {
        inventoryView.Init(this);
        equipmentView.Init(this);

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        UIState.SetInventoryOpen(false);

        inventoryModel.AddItem(wood, 50);
        inventoryModel.AddItem(stone, 25);
        inventoryModel.AddItem(smileArmor, 1);

        RefreshView();


    }
    private void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnInventoryPressed += ToggleInventory;
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnInventoryPressed -= ToggleInventory;
        }
    }

    private void ToggleInventory()
    {
        if (PauseMenuManager.IsMenuOpen)
            return;

        isOpen = !isOpen;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isOpen);
        }

        UIState.SetInventoryOpen(isOpen);

        if (isOpen && playerPresenter != null)
        {
            playerPresenter.StopMove();
        }

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

    // =========================
    // 인벤토리 슬롯 클릭
    // =========================

    public void OnInventorySlotClicked(int slotIndex)
    {
        ItemStack clickedItem = inventoryModel.Items[slotIndex];

        // 드래그 중이면 위치 교환
        if (draggingItem != null)
        {
            inventoryModel.SwapItems(
                draggedSlotIndex,
                slotIndex
            );

            StopDrag();

            RefreshView();

            return;
        }

        // 빈 슬롯
        if (clickedItem == null)
            return;

        StartDrag(
            clickedItem,
            slotIndex
        );
    }

    // =========================
    // 인벤토리 툴팁
    // =========================

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

    // =========================
    // 드래그
    // =========================

    private void StartDrag(
        ItemStack item,
        int slotIndex
    )
    {
        draggingItem = item;
        draggedSlotIndex = slotIndex;

        inventoryView.ShowDragIcon(item);
    }

    private void StopDrag()
    {
        draggingItem = null;
        draggedSlotIndex = -1;

        inventoryView.HideDragIcon();
    }

    // =========================
    // 장비 슬롯 (임시)
    // =========================

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
        EquipmentSlotView slotView =
            equipmentView.Slots[slotIndex];

        if (draggingItem.item.itemType
            != slotView.EquipType)
        {
            Debug.Log("장착 불가");
            return;
        }

        ItemStack equippedItem =
            equipmentModel.GetEquippedItem(
                slotView.EquipType
            );

        // 기존 장비 반환
        if (equippedItem != null)
        {
            inventoryModel.AddItem(
                equippedItem.item,
                equippedItem.amount
            );
        }

        equipmentModel.Equip(
            slotView.EquipType,
            draggingItem
        );

        inventoryModel.RemoveItemAt(
            draggedSlotIndex
        );

        StopDrag();

        RefreshView();
    }
    private void Unequip(int slotIndex)
    {
        EquipmentSlotView slotView =
            equipmentView.Slots[slotIndex];

        ItemStack equippedItem =
            equipmentModel.GetEquippedItem(
                slotView.EquipType
            );

        if (equippedItem == null)
            return;

        inventoryModel.AddItem(
            equippedItem.item,
            equippedItem.amount
        );

        equipmentModel.Unequip(
            slotView.EquipType
        );

        RefreshView();
    }
}