using System.Collections.Generic;
using UnityEngine;
using System;

public class InventoryModel : MonoBehaviour
{
    public event Action OnInventoryChanged;
    [SerializeField] private int maxSlots = 40;

    public List<ItemStack> Items { get; private set; } = new();

    private void Awake()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            Items.Add(null);
        }
    }

   
    public void SetItemAt(int index, ItemStack item)
    {
        if (index < 0 || index >= Items.Count) return;

        Items[index] = item;
        OnInventoryChanged?.Invoke();
    }

    public bool AddItem(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return false;

        // 전체 수량이 들어갈 공간이 있는지 먼저 검사
        if (!CanAddItem(item, amount))
            return false;

        for (int i = 0; i < Items.Count; i++)
        {
            ItemStack stack = Items[i];

            if (stack != null &&
                stack.item == item &&
                stack.amount < item.maxStack)
            {
                int space = item.maxStack - stack.amount;
                int addAmount = Mathf.Min(space, amount);

                stack.amount += addAmount;
                amount -= addAmount;

                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        // 아래쪽의 빈 슬롯에 추가하는 기존 코드는 그대로 유지

        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] == null)
            {
                int addAmount = Mathf.Min(item.maxStack, amount);
                Items[i] = new ItemStack(item, addAmount);
                amount -= addAmount;

                if (amount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        return false;
    }

    public void RemoveItemAt(int index)
    {
        if (index < 0 || index >= Items.Count) return;

        Items[index] = null;
        OnInventoryChanged?.Invoke();

    }

    public void SwapItems(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Items.Count) return;
        if (toIndex < 0 || toIndex >= Items.Count) return;

        ItemStack temp = Items[fromIndex];
        Items[fromIndex] = Items[toIndex];
        Items[toIndex] = temp;
        OnInventoryChanged?.Invoke();

    }

    public void SortItems()
    {
        List<ItemStack> sorted = new();

        foreach (ItemStack stack in Items)
        {
            if (stack != null)
                sorted.Add(stack);
        }

        sorted.Sort((a, b) =>
        {
            int typeCompare = a.item.itemType.CompareTo(b.item.itemType);
            if (typeCompare != 0) return typeCompare;

            return a.item.itemID.CompareTo(b.item.itemID);
        });

        for (int i = 0; i < Items.Count; i++)
        {
            Items[i] = i < sorted.Count ? sorted[i] : null;
        }
        OnInventoryChanged?.Invoke();

    }
    public ItemStack GetItem(int index)
    {
        if (index < 0 || index >= Items.Count)
            return null;

        return Items[index];
    }

    // (경민) 0707 NPC 퀘스트 관련 코드 추가
    public int GetItemCount(ItemData item)
    {
        if(item == null)
            return 0;

        int count = 0;

        foreach(ItemStack stack in Items)
        {
             if(stack != null && stack.item == item)
            {
                count += stack.amount;
            }
        }

        return count;
    }

    // (경민) 0707 NPC 퀘스트 관련 코드 추가
    public bool HasItem(ItemData item, int amount)
    {
        if(item == null || amount <= 0) 
            return false;

        return GetItemCount(item) >= amount;
    }

    // (경민) 0707 NPC 퀘스트 관련 코드 추가
    public bool CanAddItem(ItemData item, int amount)
    {
        if(item == null || amount <= 0)
            return false;

        int remainingAmount = amount;

        for(int i = 0; i < Items.Count; i++)
        {
            ItemStack stack = Items[i];

            if(stack != null && stack.item == item)
            {
                int space = item.maxStack - stack.amount;
                remainingAmount -= space;

                if(remainingAmount <= 0)
                    return true;
            }
        }

        for(int i = 0; i < Items.Count; i++)
        {
            if (Items[i] == null)
            {
                remainingAmount -= item.maxStack;

                if(remainingAmount <= 0)
                    return true;
            }
        }

        return false;
    }

    // (경민) 0707 NPC 퀘스트 관련 코드 추가
    public bool RemoveItem(ItemData item, int amount)
    {
        if(item == null || amount <= 0)
            return false;

        if(!HasItem(item, amount))
            return false;

        int remainingAmount = amount;

        for(int i = 0; i < Items.Count; i++)
        {
            ItemStack stack = Items[i];

            if (stack == null || stack.item != item)
                continue;

            int removeAmount = Mathf.Min(stack.amount, remainingAmount);

            stack.amount -= removeAmount;
            remainingAmount -= removeAmount;

            if(stack.amount <= 0)
                Items[i] = null;

            if(remainingAmount <= 0)
            {
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }
    public void ClearAllItems()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i] = null;
        }

        OnInventoryChanged?.Invoke();
    }
}