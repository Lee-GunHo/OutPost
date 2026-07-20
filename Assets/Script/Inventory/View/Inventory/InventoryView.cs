using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private ItemSlotView slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private int slotCount = 40;

    [Header("Drag")]
    [SerializeField] private Image dragIcon;

    [Header("Tooltip")]
    [SerializeField] private ItemTooltipView tooltipView;

    private readonly List<ItemSlotView> slots = new();

    public IReadOnlyList<ItemSlotView> Slots => slots;

    public void Init(InventoryPresenter presenter)
    {
        CreateSlots(presenter);
        dragIcon.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (dragIcon.gameObject.activeSelf)
        {
            dragIcon.transform.position = Input.mousePosition;
        }
    }

    private void CreateSlots(InventoryPresenter presenter)
    {
        slots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            ItemSlotView slotView = Instantiate(slotPrefab, slotParent);

            slotView.Initialize(SlotType.Inventory, i);

            slotView.OnSlotClicked += presenter.OnItemSlotClicked;
            slotView.OnSlotSplitClicked += presenter.OnItemSlotSplitClicked;
            slotView.OnSlotHovered += presenter.OnItemSlotHovered;
            slotView.OnSlotUnhovered += presenter.OnItemSlotUnhovered;

            slots.Add(slotView);
        }
    }

    public void Refresh(IReadOnlyList<ItemStack> items)
    {
        foreach (ItemSlotView slot in slots)
        {
            slot.Clear();
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (i >= slots.Count) break;

            if (items[i] != null)
            {
                slots[i].SetItem(items[i]);
            }
        }
    }

    public void ShowDragIcon(ItemStack item)
    {
        dragIcon.sprite = item.item.icon;
        dragIcon.gameObject.SetActive(true);
        dragIcon.transform.SetAsLastSibling();
    }

    public void HideDragIcon()
    {
        dragIcon.sprite = null;
        dragIcon.gameObject.SetActive(false);
    }

    public void ShowTooltip(ItemStack item)
    {
        tooltipView.Show(item);
    }

    public void HideTooltip()
    {
        tooltipView.Hide();
    }
}