using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public int slotCount = 30;
    public List<ItemStack> slots = new();

    public ItemData testItem;
    public int testAmount = 25;

    private void Awake()
    {
        for (int i = 0; i < slotCount; i++)
        {
            slots.Add(new ItemStack(null, 0));
        }

        if (testItem != null)
        {
            AddItem(testItem, testAmount);
        }
    }

    public int AddItem(ItemData item, int amount)
    {
        foreach (var slot in slots)
        {
            if (slot.item != item) continue;

            int space = item.maxStack - slot.amount;
            int add = Mathf.Min(space, amount);

            slot.amount += add;
            amount -= add;

            if (amount <= 0)
                return 0;
        }

        foreach (var slot in slots)
        {
            if (!slot.IsEmpty) continue;

            int add = Mathf.Min(item.maxStack, amount);
            slot.item = item;
            slot.amount = add;
            amount -= add;

            if (amount <= 0)
                return 0;
        }

        return amount;
    }
}
