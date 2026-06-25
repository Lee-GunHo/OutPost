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

    private void Awake()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        currentHp = Mathf.Clamp(currentHp - damage, 0, maxHp);
    }
}