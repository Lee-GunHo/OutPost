using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Header("Health Data")]
    [SerializeField] private int baseMaxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("MP Data")]
    [SerializeField] private int baseMaxMp = 100;
    [SerializeField] private int currentMp = 100;

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
    [SerializeField, Min(0f)] private float attackDuration = 0.175f;
    [SerializeField] private float hitDuration = 0.25f;

    [Header("Attack Range Data")]
    [SerializeField] private Vector3 attackBoxHalfSize = new Vector3(0.75f, 0.75f, 0.75f);
    [SerializeField] private float attackBoxDistance = 1.2f;

    [Header("Tool Data")]
    [SerializeField] private ToolType currentToolType = ToolType.Pickaxe;

    [Header("Wall Break Data")]
    [SerializeField] private float breakRange = 5f;
    [SerializeField, Min(0f)] private float wallBreakDuration = 0.175f;

    [Header("Stat Upgrade Data")]
    [SerializeField] private int hpIncreasePerUpgrade = 10;
    [SerializeField] private int mpIncreasePerUpgrade = 10;
    [SerializeField] private int attackIncreasePerUpgrade = 2;
    [SerializeField] private int defenseIncreasePerUpgrade = 1;
    [SerializeField] private float moveSpeedIncreasePerUpgrade = 0.2f;

    // 현재 업그레이드 레벨
    private int hpUpgradeLevel;
    private int mpUpgradeLevel;
    private int attackUpgradeLevel;
    private int defenseUpgradeLevel;
    private int moveSpeedUpgradeLevel;

    // 장비 보정
    private int equipmentHpBonus;
    private int equipmentMpBonus;
    private int equipmentAttackBonus;
    private int equipmentDefenseBonus;
    private float equipmentMoveSpeedBonus;

    // 상태 효과 추가 공격력, 방어력
    private int statusAttackBonus;
    private int statusDefenseBonus;

    public ToolType CurrentToolType => currentToolType;
    public bool IsPickaxeMode => currentToolType == ToolType.Pickaxe;
    public bool IsWeaponMode => currentToolType == ToolType.Sword;

    public float BreakRange => breakRange;

    public int MaxHp => Mathf.Max(1, baseMaxHp + equipmentHpBonus + hpUpgradeLevel * hpIncreasePerUpgrade);
    public int CurrentHp => currentHp;

    public int MaxMp => Mathf.Max(1, baseMaxMp + equipmentMpBonus + mpUpgradeLevel * mpIncreasePerUpgrade);
    public int CurrentMp => currentMp;

    public float MoveSpeed =>
    baseMoveSpeed + equipmentMoveSpeedBonus + moveSpeedUpgradeLevel * moveSpeedIncreasePerUpgrade;

    public int AttackPower =>
        baseAttackPower + equipmentAttackBonus + statusAttackBonus + attackUpgradeLevel * attackIncreasePerUpgrade;

    public int DefensePower =>
        baseDefensePower + equipmentDefenseBonus + statusDefenseBonus + defenseUpgradeLevel * defenseIncreasePerUpgrade;

    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;
    public float CurrentDashCooldown => currentDashCooldown;

    public float InteractionRange => interactionRange;

    public float AttackDuration => attackDuration;
    public float WallBreakDuration => wallBreakDuration;
    public float HitDuration => hitDuration;

    public Vector3 AttackBoxHalfSize => attackBoxHalfSize;
    public float AttackBoxDistance => attackBoxDistance;

    public bool IsDead => currentHp <= 0;
    public bool CanDash => currentDashCooldown <= 0f;

    public int HpUpgradeLevel => hpUpgradeLevel;
    public int MpUpgradeLevel => mpUpgradeLevel;
    public int AttackUpgradeLevel => attackUpgradeLevel;
    public int DefenseUpgradeLevel => defenseUpgradeLevel;
    public int MoveSpeedUpgradeLevel => moveSpeedUpgradeLevel;

    private void Awake()
    {
        ClampCurrentHp();
        ClampCurrentMp();
    }

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

    private void ClampCurrentHp()
    {
        currentHp = Mathf.Clamp(currentHp, 0, MaxHp);
    }

    private void ClampCurrentMp()
    {
        currentMp = Mathf.Clamp(currentMp, 0, MaxMp);
    }

    public void LoadSavedHealth(int savedHp)
    {
        currentHp = savedHp;
        ClampCurrentHp();
    }

    public void LoadSavedMp(int savedMp)
    {
        currentMp = savedMp;
        ClampCurrentMp();
    }

    public void LoadSavedStats(int savedAttackPower, int savedDefensePower)
    {
        baseAttackPower = savedAttackPower;
        baseDefensePower = savedDefensePower;
    }

    public void StartDashCooldown()
    {
        currentDashCooldown = dashCooldown;
    }

    public void TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage - DefensePower, 1);

        currentHp -= finalDamage;
        ClampCurrentHp();
    }

    public void TakeStatusDamage(int damage)
    {
        int finalDamage = Mathf.Max(damage, 1);

        currentHp -= finalDamage;
        ClampCurrentHp();
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHp += amount;
        ClampCurrentHp();
    }

    public bool UseMp(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentMp < amount)
        {
            return false;
        }

        currentMp -= amount;
        ClampCurrentMp();

        return true;
    }

    public void RecoverMp(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentMp += amount;
        ClampCurrentMp();
    }

    public void UpgradeHp()
    {
        hpUpgradeLevel++;

        currentHp += hpIncreasePerUpgrade;
        ClampCurrentHp();

        Debug.Log($"HP 강화 완료. 강화 레벨: {hpUpgradeLevel}, 현재 HP: {currentHp}/{MaxHp}");
    }

    public void UpgradeMp()
    {
        mpUpgradeLevel++;

        currentMp += mpIncreasePerUpgrade;
        ClampCurrentMp();

        Debug.Log($"MP 강화 완료. 강화 레벨: {mpUpgradeLevel}, 현재 MP: {currentMp}/{MaxMp}");
    }

    public void UpgradeAttack()
    {
        attackUpgradeLevel++;

        Debug.Log($"공격력 강화 완료. 강화 레벨: {attackUpgradeLevel}, 현재 공격력: {AttackPower}");
    }

    public void UpgradeDefense()
    {
        defenseUpgradeLevel++;

        Debug.Log($"방어력 강화 완료. 강화 레벨: {defenseUpgradeLevel}, 현재 방어력: {DefensePower}");
    }

    public void UpgradeMoveSpeed()
    {
        moveSpeedUpgradeLevel++;

        Debug.Log($"이동속도 강화 완료. 강화 레벨: {moveSpeedUpgradeLevel}, 현재 이동속도: {MoveSpeed}");
    }

    public void ToggleTool()
    {
        currentToolType = currentToolType == ToolType.Pickaxe
            ? ToolType.Sword
            : ToolType.Pickaxe;

        Debug.Log("현재 도구 상태: " + currentToolType);
    }

    public void LoadStatUpgradeLevels(
        int savedHpUpgradeLevel,
        int savedMpUpgradeLevel,
        int savedAttackUpgradeLevel,
        int savedDefenseUpgradeLevel,
        int savedMoveSpeedUpgradeLevel
    )
    {
        hpUpgradeLevel = Mathf.Max(0, savedHpUpgradeLevel);
        mpUpgradeLevel = Mathf.Max(0, savedMpUpgradeLevel);
        attackUpgradeLevel = Mathf.Max(0, savedAttackUpgradeLevel);
        defenseUpgradeLevel = Mathf.Max(0, savedDefenseUpgradeLevel);
        moveSpeedUpgradeLevel = Mathf.Max(0, savedMoveSpeedUpgradeLevel);

        ClampCurrentHp();
        ClampCurrentMp();
    }

    // 장비 능력치 구현 ( 이건호 ) 
    public void AddEquipmentStats(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        equipmentHpBonus += item.hpBonus;
        equipmentAttackBonus += item.attackBonus;
        equipmentDefenseBonus += item.defenseBonus;
        equipmentMoveSpeedBonus += item.moveSpeedBonus;

        ClampCurrentHp();
        ClampCurrentMp();

        Debug.Log("==========아이템 장착==========");
        Debug.Log($"장착한 아이템 능력치 - 체력:{item.hpBonus}, 공격력:{item.attackBonus}, 방어력:{item.defenseBonus}, 이동속도:{item.moveSpeedBonus}");
        Debug.Log($"추가된 아이템 능력치 - 체력:{equipmentHpBonus}, 공격력:{equipmentAttackBonus}, 방어력:{equipmentDefenseBonus}, 이동속도:{equipmentMoveSpeedBonus}");
        Debug.Log($"장비 장착 현재 능력치 - 체력:{MaxHp}, MP:{MaxMp}, 공격력:{AttackPower}, 방어력:{DefensePower}, 이동속도:{MoveSpeed}");
    }

    // 장비 능력치 제거 구현 ( 이건호 )
    public void RemoveEquipmentStats(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        equipmentHpBonus -= item.hpBonus;
        equipmentAttackBonus -= item.attackBonus;
        equipmentDefenseBonus -= item.defenseBonus;
        equipmentMoveSpeedBonus -= item.moveSpeedBonus;

        ClampCurrentHp();
        ClampCurrentMp();

        Debug.Log("==========아이템 해제==========");
        Debug.Log($"해제한 아이템 능력치 - 체력:{item.hpBonus}, 공격력:{item.attackBonus}, 방어력:{item.defenseBonus}, 이동속도:{item.moveSpeedBonus}");
        Debug.Log($"감소한 아이템 능력치 - 체력:{equipmentHpBonus}, 공격력:{equipmentAttackBonus}, 방어력:{equipmentDefenseBonus}, 이동속도:{equipmentMoveSpeedBonus}");
        Debug.Log($"장비 해제 현재 능력치 - 체력:{MaxHp}, MP:{MaxMp}, 공격력:{AttackPower}, 방어력:{DefensePower}, 이동속도:{MoveSpeed}");
    }

    public void AddStatusStats(int attackModifier, int defenseModifier)
    {
        statusAttackBonus += attackModifier;
        statusDefenseBonus += defenseModifier;

        Debug.Log($"상태효과 스탯 적용 - 공격력 변화:{attackModifier}, 방어력 변화:{defenseModifier}");
        Debug.Log($"현재 상태효과 보정 - 공격력:{statusAttackBonus}, 방어력:{statusDefenseBonus}");
    }

    public void RemoveStatusStats(int attackModifier, int defenseModifier)
    {
        statusAttackBonus -= attackModifier;
        statusDefenseBonus -= defenseModifier;

        Debug.Log($"상태효과 스탯 제거 - 공격력 변화:{attackModifier}, 방어력 변화:{defenseModifier}");
        Debug.Log($"현재 상태효과 보정 - 공격력:{statusAttackBonus}, 방어력:{statusDefenseBonus}");
    }
}
