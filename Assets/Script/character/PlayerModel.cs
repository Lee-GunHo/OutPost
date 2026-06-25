using UnityEngine;
using static Unity.VisualScripting.Dependencies.Sqlite.SQLite3;

public class PlayerModel : MonoBehaviour
{
    [Header("Health Data")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("Move Data")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash Data")]
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float currentDashCooldown = 0f;

    [Header("Interaction Data")]
    [SerializeField] private float interactionRange = 1.5f;

    [Header("Combat Data")]
    [SerializeField] private int attackPower = 10;
    [SerializeField] private int defensePower = 0;
    [SerializeField] private float attackDuration = 0.35f;

    [Header("Attack Range Data")]
    [SerializeField] private Vector3 attackBoxHalfSize = new Vector3(0.75f, 0.75f, 0.75f);
    [SerializeField] private float attackBoxDistance = 1.2f;

    [Header("Tool Data")]
    [SerializeField] private ToolType currentToolType = ToolType.Pickaxe;

    [Header("Wall Break Data")]
    [SerializeField] private float breakRange = 5f;

    public ToolType CurrentToolType => currentToolType;
    public bool IsPickaxeMode => currentToolType == ToolType.Pickaxe;
    public bool IsWeaponMode => currentToolType == ToolType.Weapon;

    public float BreakRange => breakRange;


    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;

    public float MoveSpeed => moveSpeed;

    public float DashSpeed => dashSpeed;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;
    public float CurrentDashCooldown => currentDashCooldown;

    public float InteractionRange => interactionRange;

    public int AttackPower => attackPower;
    public int DefensePower => defensePower;
    public float AttackDuration => attackDuration;

    public Vector3 AttackBoxHalfSize => attackBoxHalfSize;
    public float AttackBoxDistance => attackBoxDistance;

    public bool IsDead => currentHp <= 0;
    public bool CanDash => currentDashCooldown <= 0f;

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
        int finalDamage = Mathf.Max(damage - defensePower, 1);

        currentHp -= finalDamage;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    public void Heal(int amount)
    {
        currentHp += amount;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    public void ToggleTool()
    {
        currentToolType = currentToolType == ToolType.Pickaxe
            ? ToolType.Weapon
            : ToolType.Pickaxe;

        Debug.Log("현재 도구 상태: " + currentToolType);
    }
}