using System;
using System.Collections.Generic;
using UnityEngine;

public class HotbarModel : MonoBehaviour
{

    public List<ItemStack> Items { get; private set; } = new();
    public int SelectedIndex { get; private set; }

    public event Action OnHotbarChanged;
    public event Action<int> OnSelectedSlotChanged;



    public void Initialize(int slotCount)
    {
        Items.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            Items.Add(null);
        }

        SelectedIndex = 0;
        OnHotbarChanged?.Invoke();
    }

    public ItemStack GetItem(int index)
    {
        if (!IsValidIndex(index)) return null;

        return Items[index];
    }

    public void SetItem(int index, ItemStack itemStack)
    {
        if (!IsValidIndex(index)) return;

        Items[index] = itemStack;
        OnHotbarChanged?.Invoke();
    }

    public void SelectSlot(int index)
    {
        if (!IsValidIndex(index)) return;

        SelectedIndex = index;
        OnSelectedSlotChanged?.Invoke(index);
    }

    public void SelectNext()
    {
        int nextIndex = SelectedIndex + 1;

        if (nextIndex >= Items.Count)
            nextIndex = 0;

        SelectSlot(nextIndex);
    }

    public void SelectPrevious()
    {
        int previousIndex = SelectedIndex - 1;

        if (previousIndex < 0)
            previousIndex = Items.Count - 1;

        SelectSlot(previousIndex);
    }

    public ItemStack GetSelectedItem()
    {
        return GetItem(SelectedIndex);
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < Items.Count;
    }

    public int FindEmptySlotIndex()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] == null || Items[i].item == null)
                return i;
        }

        return -1;
    }

    public bool AddItem(ItemStack itemStack)
    {
        int emptyIndex = FindEmptySlotIndex();

        if (emptyIndex == -1)
            return false;

        SetItem(emptyIndex, itemStack);
        return true;
    }
    /// <summary>
    /// 같은 아이템 칸의 남은 공간 + 빈 칸 수용량. 이만큼은 AddItemUpTo로 넣을 수 있음
    /// </summary>
    public int GetAddableAmount(ItemData item)
    {
        if (item == null)
            return 0;

        int maxStack = Mathf.Max(1, item.maxStack);
        int addable = 0;

        foreach (ItemStack stack in Items)
        {
            if (stack == null || stack.item == null)
                addable += maxStack;
            else if (stack.item == item)
                addable += Mathf.Max(0, maxStack - stack.amount);
        }

        return addable;
    }

    /// <summary>
    /// 같은 아이템 칸에 먼저 쌓고, 남으면 앞쪽 빈 칸부터 채움. 실제로 넣은 개수를 반환
    /// </summary>
    public int AddItemUpTo(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return 0;

        int maxStack = Mathf.Max(1, item.maxStack);
        int remaining = amount;

        for (int i = 0; i < Items.Count && remaining > 0; i++)
        {
            ItemStack stack = Items[i];

            if (stack == null || stack.item != item || stack.amount >= maxStack)
                continue;

            int add = Mathf.Min(maxStack - stack.amount, remaining);
            stack.amount += add;
            remaining -= add;
        }

        for (int i = 0; i < Items.Count && remaining > 0; i++)
        {
            if (Items[i] != null && Items[i].item != null)
                continue;

            int add = Mathf.Min(maxStack, remaining);
            Items[i] = new ItemStack(item, add);
            remaining -= add;
        }

        int added = amount - remaining;

        if (added > 0)
            OnHotbarChanged?.Invoke();

        return added;
    }

    public int GetItemCount(ItemData item)
    {
        if (item == null)
            return 0;

        int count = 0;

        foreach (ItemStack stack in Items)
        {
            if (stack != null && stack.item == item)
                count += stack.amount;
        }

        return count;
    }

    // 제작 시 인벤토리와 합산해서 재료로 쓸 수 있도록 최대 amount만큼 차감하고 실제 차감량을 반환
    public int RemoveUpTo(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return 0;

        int removed = 0;

        for (int i = 0; i < Items.Count && removed < amount; i++)
        {
            ItemStack stack = Items[i];

            if (stack == null || stack.item != item)
                continue;

            int removeAmount = Mathf.Min(stack.amount, amount - removed);

            stack.amount -= removeAmount;
            removed += removeAmount;

            if (stack.amount <= 0)
                Items[i] = null;
        }

        if (removed > 0)
            OnHotbarChanged?.Invoke();

        return removed;
    }

    public void ClearAllItems()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i] = null;
        }

        SelectedIndex = 0;

        OnHotbarChanged?.Invoke();
        OnSelectedSlotChanged?.Invoke(SelectedIndex);
    }

    // 0813 경민 선택 아이템 1개 차감 기능 추가
    public bool ConsumeSelectedItem(int amount = 1)
    {
        if (amount <= 0)
            return false;

        ItemStack stack = GetSelectedItem();

        if (stack == null ||
            stack.item == null ||
            stack.amount < amount)
        {
            return false;
        }

        stack.amount -= amount;

        if (stack.amount <= 0)
        {
            Items[SelectedIndex] = null;
        }

        OnHotbarChanged?.Invoke();
        return true;
    }
}