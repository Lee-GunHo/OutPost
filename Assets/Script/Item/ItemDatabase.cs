using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ItemDatabase",
    menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField]
    private List<ItemData> items = new();

    private Dictionary<int, ItemData> itemDictionary;

    private void OnEnable()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        itemDictionary = new Dictionary<int, ItemData>();

        foreach (ItemData item in items)
        {
            if (item == null)
                continue;

            if (itemDictionary.ContainsKey(item.itemID))
            {
                Debug.LogError(
                    $"중복된 Item ID: {item.itemID}");
                continue;
            }

            itemDictionary.Add(item.itemID, item);
        }
    }

    public ItemData GetItemByID(int itemID)
    {
        if (itemDictionary == null)
            BuildDictionary();

        if (itemDictionary.TryGetValue(
                itemID,
                out ItemData item))
        {
            return item;
        }

        Debug.LogWarning(
            $"존재하지 않는 Item ID: {itemID}");

        return null;
    }
}