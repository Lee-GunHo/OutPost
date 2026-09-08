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
public enum ToolType
{
    None,
    Pickaxe,
    Axe,
    Sword,
    Hammer,
    Hoe,
    Placeable,
    Consumable
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
    [Header("Item Info")]
    public int itemID;
    public string itemName;
    public ItemType itemType;
    public ItemGrade itemGrade;
    public Sprite icon;

    [Header("Equipment Stats")]
    public int hpBonus;
    public int attackBonus;
    public int defenseBonus;
    public float moveSpeedBonus;

    [Header("Weapon Attack Timing")]
    [Tooltip("공격 후딜(초). 0이면 플레이어 기본값 사용. 벽 파괴에는 적용하지 않습니다.")]
    [Min(0f)] public float attackRecoveryDuration;





    public ToolType toolType;
    [TextArea]
    public string description;

    [Header("Placeable Block")]
    [Tooltip("ToolType이 Placeable일 때 월드에 생성할 벽/블록 프리팹")]
    public GameObject placeablePrefab;

    [Tooltip("바닥 기준 설치 높이. 기존 벽의 wallYOffset과 같은 값으로 맞추세요.")]
    public float placeableYOffset = 1f;


    public int maxStack = 99;

}
