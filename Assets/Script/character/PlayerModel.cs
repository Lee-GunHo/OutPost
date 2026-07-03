using UnityEngine;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;
using static UnityEditor.Progress;

public class PlayerModel : MonoBehaviour
{
    [Header("Health Data")]
    [SerializeField] private int baseMaxHp = 100;
    [SerializeField] private int currentHp = 100;

    //이거는 너코드

    [Header("Move Data")]
    [SerializeField] private float baseMoveSpeed = 5f;

    [Header("Dash Data")]
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float currentDashCooldown = 0f;

    // (경민) 1.5f에서 2f로 변경
    [Header("Interaction Data")]
    [SerializeField] private float interactionRange = 2f;

    [Header("Combat Data")]
    [SerializeField] private int baseAttackPower = 10;
    [SerializeField] private int baseDefensePower = 0;
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float hitDuration = 0.25f;

    [Header("Attack Range Data")]
    [SerializeField] private Vector3 attackBoxHalfSize = new Vector3(0.75f, 0.75f, 0.75f);
    [SerializeField] private float attackBoxDistance = 1.2f;

    [Header("Tool Data")]
    [SerializeField] private ToolType currentToolType = ToolType.Pickaxe;

    [Header("Wall Break Data")]
    [SerializeField] private float breakRange = 5f;

    public ToolType CurrentToolType => currentToolType;
    public bool IsPickaxeMode => currentToolType == ToolType.Pickaxe;
    public bool IsWeaponMode => currentToolType == ToolType.Sword;

    public float BreakRange => breakRange;



    
    public int MaxHp => baseMaxHp + equipmentHpBonus;
    public int CurrentHp => currentHp;

    public float MoveSpeed => baseMoveSpeed + equipmentMoveSpeedBonus;
    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;
    public float CurrentDashCooldown => currentDashCooldown;

    public float InteractionRange => interactionRange;

    public int AttackPower => baseAttackPower + equipmentAttackBonus;
    public int DefensePower => baseDefensePower + equipmentDefenseBonus;
    public float AttackDuration => attackDuration;
    public float HitDuration => hitDuration;

    public Vector3 AttackBoxHalfSize => attackBoxHalfSize;
    public float AttackBoxDistance => attackBoxDistance;

    public bool IsDead => currentHp <= 0;
    public bool CanDash => currentDashCooldown <= 0f;
   
    private int equipmentHpBonus;
    private int equipmentAttackBonus;
    private int equipmentDefenseBonus;
    private float equipmentMoveSpeedBonus;
    private void Update()
    {
        UpdateDashCooldown();
    }

    private void UpdateDashCooldown()
    {
        if (currentDashCooldown <= 0f)
        {
            return;
        }

        currentDashCooldown -= Time.deltaTime;

        if (currentDashCooldown < 0f)
        {
            currentDashCooldown = 0f;
        }
    }

    public void StartDashCooldown()
    {
        currentDashCooldown = dashCooldown;
    }

    public void TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage - DefensePower, 1);
            
        currentHp -= finalDamage;
        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);
    }

    public void Heal(int amount)
    {
        currentHp += amount;
        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);
    }

    public void ToggleTool()
    {
        currentToolType = currentToolType == ToolType.Pickaxe
            ? ToolType.Sword
            : ToolType.Pickaxe;

        Debug.Log("현재 도구 상태: " + currentToolType);
    }


    // 장비 능력치 구현 ( 이건호 ) 
    public void AddEquipmentStats(ItemData item)
    {
        if (item == null) return;

        equipmentHpBonus += item.hpBonus;
        equipmentAttackBonus += item.attackBonus;
        equipmentDefenseBonus += item.defenseBonus;
        equipmentMoveSpeedBonus += item.moveSpeedBonus;

        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);

        Debug.Log($"장착한 아이템 능력치 - 체력 : {item.hpBonus}, 공격력 : {item.attackBonus}, 방어력 : {item.defenseBonus}, 이동속도 : {item.moveSpeedBonus}");
        Debug.Log($"추가된 아이템 능력치 - 체력 : {equipmentHpBonus}, 공격력 : {equipmentAttackBonus}, 방어력 : {equipmentDefenseBonus}, 이동속도 : {equipmentMoveSpeedBonus}");
        Debug.Log($"장비 장착 현재 능력치 - 체력:{MaxHp}, 공격력:{AttackPower}, 방어력:{DefensePower}, 이동속도:{MoveSpeed}");
    }
    // 장비 능력치 제거 구현 ( 이건호 )
    public void RemoveEquipmentStats(ItemData item)
    {
        if (item == null) return;
        equipmentHpBonus -= item.hpBonus;
        equipmentAttackBonus -= item.attackBonus;
        equipmentDefenseBonus -= item.defenseBonus;
        equipmentMoveSpeedBonus -= item.moveSpeedBonus;

        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);

        Debug.Log($"해제한 아이템 능력치 - 체력 : {item.hpBonus}, 공격력 : {item.attackBonus}, 방어력 : {item.defenseBonus}, 이동속도 : {item.moveSpeedBonus}");
        Debug.Log($"감소한 아이템 능력치 - 체력 : {equipmentHpBonus}, 공격력 : {equipmentAttackBonus}, 방어력 : {equipmentDefenseBonus}, 이동속도 : {equipmentMoveSpeedBonus}");
        Debug.Log($"장비 해제 현재 능력치 - 체력:{MaxHp}, 공격력:{AttackPower}, 방어력:{DefensePower}, 이동속도:{MoveSpeed}");
    }
}