using System.Collections.Generic;
using UnityEngine;

public class EquipmentView : MonoBehaviour
{
    [SerializeField] private List<EquipmentSlotView> slots;

    public IReadOnlyList<EquipmentSlotView> Slots => slots;

    public void Init(InventoryPresenter presenter)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Init(i, presenter);
        }
    }

    public EquipmentSlotView GetSlot(ItemType type)
    {
        foreach (EquipmentSlotView slot in slots)
        {
            if (slot.EquipType == type)
                return slot;
        }

        return null;
    }

    public void Refresh(EquipmentModel equipmentModel)
    {
        foreach (EquipmentSlotView slot in slots)
        {
            ItemStack item = equipmentModel.GetEquippedItem(slot.EquipType);

            if (item == null)
                slot.Clear();
            else
                slot.SetItem(item);
        }
    }

    public void UpdateSlot(ItemType type, ItemStack item)
    {
        EquipmentSlotView slot = GetSlot(type);

        if (slot == null)
            return;

        slot.SetItem(item);
    }
}