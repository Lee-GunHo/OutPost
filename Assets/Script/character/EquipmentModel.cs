using UnityEngine;

public class EquipmentModel : MonoBehaviour
{
    [SerializeField] private ItemStack weapon;
    [SerializeField] private ItemStack armor;
    [SerializeField] private ItemStack shoes;
    [SerializeField] private ItemStack ring;
    [SerializeField] private ItemStack necklace;

    public ItemStack Weapon => weapon;
    public ItemStack Armor => armor;
    public ItemStack Shoes => shoes;
    public ItemStack Ring => ring;
    public ItemStack Necklace => necklace;

    public ItemStack GetEquippedItem(ItemType type)
    {
        return type switch
        {
            ItemType.Weapon => weapon,
            ItemType.Armor => armor,
            ItemType.Shoes => shoes,
            ItemType.Ring => ring,
            ItemType.Necklace => necklace,
            _ => null
        };
    }
    
    public void Equip(ItemType type, ItemStack item)
    {
        switch (type)
        {
            case ItemType.Weapon:
                weapon = item;
                break;
            case ItemType.Armor:
                armor = item;
                break;
            case ItemType.Shoes:
                shoes = item;
                break;
            case ItemType.Ring:
                ring = item;
                break;
            case ItemType.Necklace:
                necklace = item;
                break;
        }
    }

    public void Unequip(ItemType type)
    {
        Equip(type, null);
    }
}