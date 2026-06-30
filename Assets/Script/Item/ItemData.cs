using UnityEngine;

public enum ItemType
{
    Weapon,
    Head,
    Armor,
    Shoes,
    Ring,
    Necklace,
    Consumable,
    Material
}

public enum ItemGrade
{
    Normal,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{

    [Header("Equipment Stats")]
    public int hpBonus;
    public int attackBonus;
    public int defenseBonus;
    public float moveSpeedBonus;




    public int itemID;
    public string itemName;
    public ItemType itemType;
    public ItemGrade itemGrade;
    public Sprite icon;
    [TextArea]
    public string description;


    public int maxStack = 99;

}