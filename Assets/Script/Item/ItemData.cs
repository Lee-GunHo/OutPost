using UnityEngine;

public enum ItemType
{
    Material,
    Tool,
    Weapon,
    Armor,
    Food
}

[CreateAssetMenu(menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public int maxStack = 99;
    public ItemType itemType;
}
