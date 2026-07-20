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

    public float MoveSpeed => monsterModel.MoveSpeed;
    public float ChaseRange => monsterModel.ChaseRange;
    public float AttackRange => monsterModel.AttackRange;
    public float HitDuration => monsterModel.HitDuration;
    public float KnockbackPower => monsterModel.KnockbackPower;
    public bool IsDead => monsterModel.IsDead;
    public int ExpReward => monsterModel.ExpReward;

    private bool isDeathProcessed;

    [Header("Navigation")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Behavior")]
    [SerializeField] private MonsterBehaviorMode behaviorMode = MonsterBehaviorMode.Wild;

    [Header("Target")]
    [SerializeField] private Transform nexusTransform;
    [SerializeField] private float playerAggroReleaseRange = 12f;

    private Transform currentTarget;
    private bool isPlayerAggro;

    public bool HasTarget => currentTarget != null;


    private void Awake()
    {
        monsterModel = GetComponent<MonsterModel>();
        stateManager = GetComponent<MonsterStateManager>();
        rigid = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null && monsterModel != null)
        {
            agent.speed = monsterModel.MoveSpeed;
            agent.stoppingDistance = monsterModel.AttackRange * 0.8f;
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
        if (playerTransform == null)
        {
            Debug.LogWarning("playerTransform이 null입니다.");
            return false;
        }

        float distance = GetDistanceToPlayer();

        return distance <= ChaseRange;
    }

    public bool IsCurrentTargetInAttackRange()
    {
        UpdateTarget();

        if (currentTarget == null)
        {
            return false;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.position);

        return distance <= AttackRange;
    }

    public float GetDistanceToPlayer()
    {
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

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("몬스터가 NavMesh 위에 있지 않습니다.");
            StopMove();
            return;
        }

        agent.isStopped = false;
        agent.speed = monsterModel.MoveSpeed;
        agent.stoppingDistance = monsterModel.AttackRange * 0.8f;
        Debug.Log("현재 타겟으로 이동: " + currentTarget.name);
        agent.SetDestination(currentTarget.position);
    }

    public void StopMove()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (rigid != null)
        {
            rigid.linearVelocity = Vector3.zero;
        }
    }

    public void KnockbackFromPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                Debug.LogWarning("넉백 기준 플레이어를 찾지 못했습니다.");
                return;
            }

            playerTransform = player.transform;
        }

        Vector3 knockbackDirection = transform.position - playerTransform.position;
        knockbackDirection.y = 0f;

        if (knockbackDirection == Vector3.zero)
        {
            knockbackDirection = -transform.forward;
        }

        knockbackDirection = knockbackDirection.normalized;

        rigid.linearVelocity = knockbackDirection * KnockbackPower;

        Debug.Log("몬스터 넉백");
    }

    public void Attack()
    {
        UpdateTarget();

        if (currentTarget == null)
        {
            Debug.LogWarning("공격할 대상이 없습니다.");
            return;
        }

        if (!IsCurrentTargetInAttackRange())
        {
            Debug.Log("대상이 공격 범위 밖입니다.");
            return;
        }

        IDamageable damageable = currentTarget.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            Debug.LogWarning("현재 타겟에게 IDamageable이 없습니다: " + currentTarget.name);
            return;
        }

        damageable.TakeDamage(monsterModel.AttackPower);

        Debug.Log("몬스터가 타겟에게 데미지 줌: " + monsterModel.AttackPower);

        if (currentTarget == playerTransform)
        {
            TryApplyStatusEffectToPlayer();
        }
    }


    public void TakeDamage(int damage)
    {
        SetTargetToPlayer();

        if (monsterModel.IsDead)
        {
            return;
        }

        monsterModel.TakeDamage(damage);

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
        SetTargetToNexus();

        Debug.Log("웨이브 몬스터로 초기화됨. 목표: Nexus");
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
        if (playerTransform == null)
        {
            return;
        }

        currentTarget = playerTransform;
        isPlayerAggro = true;
    }

    public void UpdateTarget()
    {
        if (playerTransform != null)
        {
            float playerDistance = Vector3.Distance(transform.position, playerTransform.position);

            // 플레이어가 인식 범위 안으로 들어오면 플레이어를 공격
            if (playerDistance <= ChaseRange)
            {
                SetTargetToPlayer();
                return;
            }

            // 플레이어를 쫓던 중 너무 멀어지면 어그로 해제
            if (isPlayerAggro && playerDistance > playerAggroReleaseRange)
            {
                isPlayerAggro = false;
            }

            // 아직 플레이어 어그로가 유지 중이면 계속 플레이어 추적
            if (isPlayerAggro)
            {
                currentTarget = playerTransform;
                return;
            }
        }

        // 웨이브 몬스터는 플레이어 어그로가 없으면 넥서스 공격
        if (behaviorMode == MonsterBehaviorMode.NexusAssault)
        {
            SetTargetToNexus();
            return;
        }

        // 자연 몬스터는 플레이어가 없으면 아무 목표 없음
        currentTarget = null;
    }

    private void OnDrawGizmosSelected()
    {
        MonsterModel model = GetComponent<MonsterModel>();

        if (model == null)
        {
            return;
        }

        // 인식 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, model.ChaseRange);

        // 공격 범위
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, model.AttackRange);
    }
}