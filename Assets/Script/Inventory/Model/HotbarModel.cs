using System;
using System.Collections.Generic;
using UnityEngine;

public class HotbarModel : MonoBehaviour
{
    [SerializeField] private int maxSlotCount = 8;

    public List<ItemStack> Items { get; private set; } = new();
    public int SelectedIndex { get; private set; }

    public event Action OnHotbarChanged;
    public event Action<int> OnSelectedSlotChanged;

    private void Awake()
    {
        Initialize(maxSlotCount);
    }

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
}