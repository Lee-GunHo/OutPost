using System.Collections.Generic;
using UnityEngine;

public class InventoryPresenter : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private InventoryModel inventoryModel;

    [Header("View")]
    [SerializeField] private InventoryView inventoryView;

    [Header("Test Items")]
    [SerializeField] private ItemData wood;
    [SerializeField] private ItemData stone;
    [SerializeField] private ItemData smileArmor;

    private ItemStack draggingItem;
    private int draggedSlotIndex = -1;

    private void Start()
    {
        inventoryView.Init(this);

        inventoryModel.AddItem(wood, 50);
        inventoryModel.AddItem(stone, 25);
        inventoryModel.AddItem(smileArmor, 1);

        RefreshView();
    }

    private void RefreshView()
    {
        inventoryView.Refresh(inventoryModel.Items);
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
        Debug.Log("장비 슬롯 클릭");
    }

    public void OnEquipmentSlotHovered(int slotIndex)
    {
    }

    public void OnEquipmentSlotUnhovered(int slotIndex)
    {
    }
}