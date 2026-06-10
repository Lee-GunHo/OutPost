using System.Collections.Generic;
using UnityEngine;

public class Hotbar : MonoBehaviour
{
    [SerializeField] private int maxSlots = 8;

    public List<ItemStack> Items { get; private set; } = new();

    private void Awake()
    {
        Items.Clear();

        for (int i = 0; i < maxSlots; i++)
        {
            Items.Add(null);
        }
    }

    public ItemStack GetItem(int index)
    {
        if (index < 0 || index >= Items.Count)
            return null;

        return Items[index];
    }

    public void SetItem(int index, ItemStack stack)
    {
        if (index < 0 || index >= Items.Count)
            return;

        Items[index] = stack;
    }
}