using UnityEngine;

public class MonsterModel : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float hitDuration = 0.3f;


    [SerializeField] private int attackPower = 10;
    [SerializeField] private int maxHp = 50;

    private int currentHp;

    public float MoveSpeed => moveSpeed;
    public float ChaseRange => chaseRange;
    public float AttackRange => attackRange;
    public float HitDuration => hitDuration;

    public int AttackPower => attackPower;
    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;

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