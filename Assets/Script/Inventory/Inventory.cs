using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private int maxSlots = 40;

    public List<ItemStack> Items { get; private set; } = new();

    private void Awake()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            Items.Add(null);
        }
    }

    public bool AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return false;

        for (int i = 0; i < Items.Count; i++)
        {
            ItemStack stack = Items[i];

            if (stack != null && stack.item == item && stack.amount < item.maxStack)
            {
                int space = item.maxStack - stack.amount;
                int addAmount = Mathf.Min(space, amount);

                stack.amount += addAmount;
                amount -= addAmount;

                if (amount <= 0)
                    return true;
            }
        }

        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] == null)
            {
                int addAmount = Mathf.Min(item.maxStack, amount);
                Items[i] = new ItemStack(item, addAmount);
                amount -= addAmount;

                if (amount <= 0)
                    return true;
            }
        }

        return false;
    }

    public void RemoveItemAt(int index)
    {
        if (index < 0 || index >= Items.Count)
            return;

        Items[index] = null;
    }

    public void SortItems()
    {
        List<ItemStack> sortedItems = new();

        foreach (ItemStack stack in Items)
        {
            if (stack != null)
                sortedItems.Add(stack);
        }

        sortedItems.Sort((a, b) =>
        {
            int typeCompare = a.item.itemType.CompareTo(b.item.itemType);
            if (typeCompare != 0) return typeCompare;

            return a.item.itemID.CompareTo(b.item.itemID);
        });

        for (int i = 0; i < Items.Count; i++)
        {
            Items[i] = i < sortedItems.Count ? sortedItems[i] : null;
        }
    }

    public void SwapItems(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Items.Count)
            return;

        if (toIndex < 0 || toIndex >= Items.Count)
            return;

        ItemStack temp = Items[fromIndex];
        Items[fromIndex] = Items[toIndex];
        Items[toIndex] = temp;
    }
}