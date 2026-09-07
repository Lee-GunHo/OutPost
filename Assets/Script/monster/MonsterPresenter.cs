using UnityEngine;
using UnityEngine.AI;

public class MonsterPresenter : MonoBehaviour, IDamageable
{
    public enum MonsterBehaviorMode
    {
        Wild,          // 자연 생성 몬스터
        NexusAssault   // 웨이브 몬스터
    }

    private MonsterModel monsterModel;
    private MonsterStateManager stateManager;
    private Rigidbody rigid;
    private Transform playerTransform;
    private MonsterView monsterView;

    public float MoveSpeed => monsterModel.MoveSpeed;
    public float ChaseRange => monsterModel.ChaseRange;
    public float AttackRange => monsterModel.AttackRange;
    public float HitDuration => monsterModel.HitDuration;
    public float KnockbackPower => monsterModel.KnockbackPower;
    public bool IsDead => monsterModel.IsDead;
    public int ExpReward => monsterModel.ExpReward;
    public int AttackPower => monsterModel.AttackPower;

    public Transform CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;
    public bool IsCurrentTargetPlayer => currentTarget == playerTransform;

    private bool isDeathProcessed;

    [Header("Navigation")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Behavior")]
    [SerializeField] private MonsterBehaviorMode behaviorMode = MonsterBehaviorMode.Wild;

    [Header("Target")]
    [SerializeField] private Transform nexusTransform;
    [SerializeField] private float playerAggroReleaseRange = 12f;
    [SerializeField, Min(0f)] private float playerHitAggroDuration = 3f;

    [Header("Knockback")]
    [SerializeField, Min(0f)] private float knockbackDuration = 0.15f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1f;

    [Header("Nexus Assault")]
    [SerializeField] private float nexusAttackRange = 2f;
    [SerializeField] private float nexusStoppingDistance = 1f;
    [SerializeField] private float nexusNavMeshSearchRadius = 5f;

    public float AttackCooldown => attackCooldown;

    private Transform currentTarget;
    private bool isPlayerAggro;
    private float playerHitAggroUntil;
    private Vector3 knockbackVelocity;
    private float knockbackTimeRemaining;
    private float activeKnockbackDuration;

    private bool CanNavigate => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    private void Awake()
    {
        monsterModel = GetComponent<MonsterModel>();
        stateManager = GetComponent<MonsterStateManager>();
        rigid = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        monsterView = GetComponent<MonsterView>();

        // NavMesh owns movement, including knockback. Physics must not move the same body.
        if (agent != null && rigid != null)
        {
            rigid.isKinematic = true;
        }

        if (monsterView == null)
        {
            monsterView = GetComponentInChildren<MonsterView>();
        }

        if (agent != null && monsterModel != null)
        {
            agent.speed = monsterModel.MoveSpeed;
            agent.stoppingDistance = 1f;
            agent.updateRotation = true;
        }
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log("플레이어 찾음: " + player.name);
        }
        else
        {
            Debug.LogWarning("Player 태그를 가진 오브젝트를 찾지 못했습니다.");
        }
    }

    public bool IsPlayerInChaseRange()
    {
        UpdateTarget();
        return HasTarget;
    }

    public bool IsCurrentTargetInAttackRange()
    {
        UpdateTarget();

        if (currentTarget == null)
        {
            return false;
        }

        if (behaviorMode == MonsterBehaviorMode.NexusAssault &&
            currentTarget == nexusTransform)
        {
            float nexusDistance = Vector3.Distance(transform.position, nexusTransform.position);
            return nexusDistance <= nexusAttackRange;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        return distance <= AttackRange;
    }

    public float GetDistanceToPlayer()
    {
        if (playerTransform == null)
        {
            return float.MaxValue;
        }

        return Vector3.Distance(transform.position, playerTransform.position);
    }

    public void ChaseTarget()
    {
        UpdateTarget();

        if (currentTarget == null)
        {
            StopMove();
            return;
        }

        if (agent == null)
        {
            Debug.LogWarning("NavMeshAgent가 없습니다.");
            StopMove();
            return;
        }

        if (!CanNavigate)
        {
            Debug.LogWarning("몬스터가 NavMesh 위에 있지 않습니다.");
            StopMove();
            return;
        }

        agent.isStopped = false;
        agent.speed = monsterModel.MoveSpeed;

        if (behaviorMode == MonsterBehaviorMode.NexusAssault && currentTarget == nexusTransform)
        {
            agent.stoppingDistance = nexusStoppingDistance;
            agent.SetDestination(GetNexusDestination());
        }
        else
        {
            agent.stoppingDistance = monsterModel.AttackRange * 0.8f;
            agent.SetDestination(currentTarget.position);
        }
    }

    private Vector3 GetNexusDestination()
    {
        if (nexusTransform == null)
        {
            return transform.position;
        }

        if (NavMesh.SamplePosition(
                nexusTransform.position,
                out NavMeshHit hit,
                nexusNavMeshSearchRadius,
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        return nexusTransform.position;
    }

    public void StopMove()
    {
        knockbackTimeRemaining = 0f;
        knockbackVelocity = Vector3.zero;

        if (CanNavigate)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        if (rigid != null && !rigid.isKinematic)
        {
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    public void KnockbackFromPlayer()
    {
        if (!TryResolvePlayer() || !CanNavigate)
        {
            return;
        }

        Vector3 knockbackDirection = transform.position - playerTransform.position;
        knockbackDirection.y = 0f;

        if (knockbackDirection == Vector3.zero)
        {
            knockbackDirection = -transform.forward;
        }

        knockbackDirection = knockbackDirection.normalized;

        activeKnockbackDuration = Mathf.Max(0f, Mathf.Min(knockbackDuration, HitDuration));
        knockbackTimeRemaining = activeKnockbackDuration;
        knockbackVelocity = knockbackDirection * Mathf.Max(0f, KnockbackPower);
    }

    public void UpdateKnockback(float deltaTime)
    {
        if (knockbackTimeRemaining <= 0f || !CanNavigate)
        {
            return;
        }

        float step = Mathf.Min(deltaTime, knockbackTimeRemaining);
        // Average speed over this step gives a short push that decelerates to zero.
        float speedFactor = (knockbackTimeRemaining - step * 0.5f) / activeKnockbackDuration;
        agent.Move(knockbackVelocity * speedFactor * step);
        knockbackTimeRemaining -= step;
    }

    // 실제 공격 처리는 MonsterAttackState에서 한다.
    // 다른 코드가 아직 이 함수를 부르고 있으면 콘솔에서 바로 확인하기 위해 남겨둔다.
    public void Attack()
    {
        Debug.LogWarning("MonsterPresenter.Attack()은 사용하지 않습니다. MonsterAttackState에서 공격을 처리하세요.");
    }

    public void TakeDamage(int damage)
    {
        if (monsterModel.IsDead)
        {
            return;
        }

        SetTargetToPlayer();
        // A hit outside the release range must still provoke a pursuit after hit recovery.
        playerHitAggroUntil = Time.time + Mathf.Max(playerHitAggroDuration, HitDuration);

        monsterModel.TakeDamage(damage);

        if (monsterView != null)
        {
            monsterView.PlayHitBlinkEffect(HitDuration);
        }

        Debug.Log("몬스터 피격, 현재 체력: " + monsterModel.CurrentHp);

        if (monsterModel.IsDead)
        {
            stateManager.ChangeState(stateManager.DeadState);
        }
        else
        {
            stateManager.ChangeState(stateManager.HitState);
        }
    }

    public void Dead()
    {
        if (isDeathProcessed)
        {
            return;
        }

        isDeathProcessed = true;

        StopMove();

        Debug.Log("몬스터 사망");

        GiveExpToPlayer();

        AddKillProgress();

        Destroy(gameObject, 1f);
    }

    private void AddKillProgress()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("GameProgressManager가 없습니다.");
            return;
        }

        GameProgressManager.Instance.AddNormalMonsterKill();
    }

    private void GiveExpToPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning("경험치를 줄 플레이어를 찾지 못했습니다.");
            return;
        }

        PlayerPresenter player = playerObject.GetComponent<PlayerPresenter>();

        if (player == null)
        {
            Debug.LogWarning("PlayerPresenter를 찾지 못했습니다.");
            return;
        }

        player.AddExp(ExpReward);

        Debug.Log("플레이어에게 경험치 지급: " + ExpReward);
    }

    public void TryApplyStatusEffectToCurrentTarget()
    {
        if (currentTarget != playerTransform)
        {
            return;
        }

        TryApplyStatusEffectToPlayer();
    }

    private void TryApplyStatusEffectToPlayer()
    {
        Debug.Log("상태효과 부여 시도");

        if (monsterModel.AttackStatusEffectType == StatusEffectType.None)
        {
            Debug.Log("몬스터 상태효과 없음");
            return;
        }

        if (Random.value > monsterModel.StatusEffectChance)
        {
            Debug.Log("상태효과 확률 실패");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("상태효과를 적용할 playerTransform이 없습니다.");
            return;
        }

        PlayerPresenter player = playerTransform.GetComponentInParent<PlayerPresenter>();

        if (player == null)
        {
            Debug.LogWarning("상태효과를 적용할 PlayerPresenter를 찾지 못했습니다.");
            return;
        }

        StatusEffectData effectData = new StatusEffectData(
            monsterModel.AttackStatusEffectType,
            monsterModel.StatusEffectDuration,
            monsterModel.StatusEffectValue,
            monsterModel.StatusEffectTickInterval,
            monsterModel.StatusAttackModifier,
            monsterModel.StatusDefenseModifier
        );

        player.AddStatusEffect(effectData);

        Debug.Log("몬스터가 상태효과 부여: " + monsterModel.AttackStatusEffectType);
    }

    public void InitializeAsWaveMonster(Transform nexus)
    {
        behaviorMode = MonsterBehaviorMode.NexusAssault;
        nexusTransform = nexus;

        isPlayerAggro = false;
        playerHitAggroUntil = 0f;
        SetTargetToNexus();

        if (agent != null)
        {
            agent.stoppingDistance = nexusStoppingDistance;
        }

        Debug.Log(
            "웨이브 몬스터로 초기화됨. 목표: " +
            (nexusTransform != null ? nexusTransform.name : "null")
        );
    }

    private void SetTargetToNexus()
    {
        if (nexusTransform == null)
        {
            currentTarget = null;
            return;
        }

        currentTarget = nexusTransform;
    }

    private void SetTargetToPlayer()
    {
        if (!TryResolvePlayer())
        {
            return;
        }

        currentTarget = playerTransform;
        isPlayerAggro = true;
    }

    private bool TryResolvePlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        return playerTransform != null;
    }

    public void UpdateTarget()
    {
        if (TryResolvePlayer())
        {
            float playerDistance = Vector3.Distance(transform.position, playerTransform.position);

            if (playerDistance <= ChaseRange)
            {
                SetTargetToPlayer();
                return;
            }

            float releaseRange = Mathf.Max(ChaseRange, playerAggroReleaseRange);
            if (isPlayerAggro && playerDistance > releaseRange && Time.time >= playerHitAggroUntil)
            {
                isPlayerAggro = false;
            }

            if (isPlayerAggro)
            {
                currentTarget = playerTransform;
                return;
            }
        }

        isPlayerAggro = false;

        if (behaviorMode == MonsterBehaviorMode.NexusAssault)
        {
            SetTargetToNexus();
        }
        else
        {
            currentTarget = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        MonsterModel model = GetComponent<MonsterModel>();

        if (model == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, model.ChaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, model.AttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, nexusAttackRange);
    }
}
