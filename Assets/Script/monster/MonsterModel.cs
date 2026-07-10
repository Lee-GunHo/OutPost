using UnityEngine;

public class MonsterModel : MonoBehaviour
{
    [Header("Move Data")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("AI Range Data")]
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Combat Data")]
    [SerializeField] private int attackPower = 10;
    [SerializeField] private int maxHp = 50;

    [Header("Hit Data")]
    [SerializeField] private float hitDuration = 0.3f;
    [SerializeField] private float knockbackPower = 5f;

    [Header("Reward Data")]
    [SerializeField] private int expReward = 20;

    [Header("Status Effect Attack Data")]
    [SerializeField] private StatusEffectType attackStatusEffectType = StatusEffectType.None;
    [SerializeField] private float statusEffectDuration = 0f;
    [SerializeField] private float statusEffectValue = 0f;
    [SerializeField] private float statusEffectTickInterval = 1f;
    [SerializeField] private int statusAttackModifier = 0;
    [SerializeField] private int statusDefenseModifier = 0;
    [SerializeField, Range(0f, 1f)] private float statusEffectChance = 0f;

    public StatusEffectType AttackStatusEffectType => attackStatusEffectType;

    public float StatusEffectDuration => statusEffectDuration;
    public float StatusEffectValue => statusEffectValue;
    public float StatusEffectTickInterval => statusEffectTickInterval;

    public int StatusAttackModifier => statusAttackModifier;
    public int StatusDefenseModifier => statusDefenseModifier;

    public float StatusEffectChance => statusEffectChance;




    private int currentHp;

    public float MoveSpeed => moveSpeed;

    public float ChaseRange => chaseRange;
    public float AttackRange => attackRange;

    public int AttackPower => attackPower;
    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;

    public float HitDuration => hitDuration;
    public float KnockbackPower => knockbackPower;

    public bool IsDead => currentHp <= 0;

    public int ExpReward => expReward;

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        currentHp = Mathf.Clamp(currentHp - damage, 0, maxHp);
    }
}