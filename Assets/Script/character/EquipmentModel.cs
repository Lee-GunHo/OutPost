using UnityEngine;

public class EquipmentModel : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private string weaponName = "None";
    [SerializeField] private int weaponAttackPower = 0;

    [Header("Armor")]
    [SerializeField] private string armorName = "None";
    [SerializeField] private int armorDefensePower = 0;

    public string WeaponName => weaponName;
    public int WeaponAttackPower => weaponAttackPower;

    public string ArmorName => armorName;
    public int ArmorDefensePower => armorDefensePower;

    public void EquipWeapon(string newWeaponName, int attackPower)
    {
        weaponName = newWeaponName;
        weaponAttackPower = attackPower;

        Debug.Log("무기 장착: " + weaponName);
    }

    public void EquipArmor(string newArmorName, int defensePower)
    {
        armorName = newArmorName;
        armorDefensePower = defensePower;

        Debug.Log("방어구 장착: " + armorName);
    }

    public void UnequipWeapon()
    {
        weaponName = "None";
        weaponAttackPower = 0;

        Debug.Log("무기 해제");
    }

    public void UnequipArmor()
    {
        armorName = "None";
        armorDefensePower = 0;

        Debug.Log("방어구 해제");
    }
}