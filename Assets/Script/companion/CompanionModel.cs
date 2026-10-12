using UnityEngine;

/// <summary>
/// 전투 동료의 수치 데이터. 전부 인스펙터에서 조정.
/// </summary>
public class CompanionModel : MonoBehaviour
{
    [Header("Move Data")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Follow Data")]
    [Tooltip("플레이어와 이 거리보다 멀어지면 따라가기 시작")]
    [SerializeField] private float followStartDistance = 4f;
    [Tooltip("따라가다가 이 거리 안으로 들어오면 멈춤")]
    [SerializeField] private float followStopDistance = 2f;
    [Tooltip("플레이어와 이 거리 이상 벌어지면 전투를 포기하고 플레이어에게 복귀")]
    [SerializeField] private float leashDistance = 14f;

    [Header("Combat Data")]
    [Tooltip("이 거리 안의 몬스터를 대상으로 삼음")]
    [SerializeField] private float detectRange = 8f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private int attackPower = 5;
    [Tooltip("공격 간격(초)")]
    [SerializeField] private float attackCooldown = 1f;
    [Tooltip("대상 몬스터를 다시 찾는 간격(초)")]
    [SerializeField] private float targetScanInterval = 0.25f;

    public float MoveSpeed => moveSpeed;
    public float DetectRange => detectRange;
    public float AttackRange => attackRange;
    public int AttackPower => attackPower;
    public float AttackCooldown => attackCooldown;
    public float TargetScanInterval => targetScanInterval;

    public CompanionRuleValues RuleValues => new CompanionRuleValues
    {
        FollowStartDistance = followStartDistance,
        FollowStopDistance = followStopDistance,
        AttackRange = attackRange,
        LeashDistance = leashDistance
    };

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        followStopDistance = Mathf.Max(0f, followStopDistance);
        followStartDistance = Mathf.Max(followStopDistance, followStartDistance);
        leashDistance = Mathf.Max(followStartDistance, leashDistance);
        detectRange = Mathf.Max(0f, detectRange);
        attackRange = Mathf.Max(0f, attackRange);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        targetScanInterval = Mathf.Max(0.05f, targetScanInterval);
    }
}
