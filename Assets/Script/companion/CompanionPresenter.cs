using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 전투 동료(전투 NPC)의 이동/탐지/공격을 담당. 상태 전이는 CompanionStateManager가 처리.
/// NPCData의 Role이 Combat인 NPC 오브젝트에 붙이고, 플레이어가 이 NPC를 발견한 뒤부터 따라다니며 싸움.
/// NavMeshAgent가 있으면 길찾기로, 없으면 직선으로 이동.
/// </summary>
[RequireComponent(typeof(CompanionModel))]
[RequireComponent(typeof(CompanionStateManager))]
public class CompanionPresenter : MonoBehaviour
{
    private CompanionModel companionModel;
    private NavMeshAgent agent;

    private Transform playerTransform;
    private MonsterPresenter currentTarget;

    private float nextTargetScanTime;
    private float nextAttackTime;
    private float nextNavMeshPlaceTime;

    public CompanionRuleValues RuleValues => companionModel.RuleValues;

    private void Awake()
    {
        companionModel = GetComponent<CompanionModel>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.speed = companionModel.MoveSpeed;
            agent.stoppingDistance = 0f;
        }
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("CompanionPresenter: Player 태그를 가진 오브젝트를 찾지 못했습니다.");
        }
    }

    /// <summary>
    /// 상태 판단(CompanionDecision)에 넘길 현재 상황을 만듦.
    /// </summary>
    public CompanionSense Sense()
    {
        bool isRecruited = playerTransform != null &&
            ProgressTracker.Instance != null &&
            ProgressTracker.Instance.IsNpcDiscovered(NPCRole.Combat.ToString());

        if (isRecruited)
        {
            UpdateTarget();
        }
        else
        {
            currentTarget = null;
        }

        return new CompanionSense
        {
            IsRecruited = isRecruited,
            DistanceToPlayer = playerTransform != null
                ? GetFlatDistance(playerTransform.position)
                : 0f,
            HasTarget = currentTarget != null,
            DistanceToTarget = currentTarget != null
                ? GetFlatDistance(currentTarget.transform.position)
                : float.MaxValue
        };
    }

    // 탐지 범위 안에서 가장 가까운 살아있는 몬스터를 대상으로 삼음 (일정 간격으로만 다시 찾음)
    private void UpdateTarget()
    {
        if (currentTarget != null &&
            (currentTarget.IsDead || GetFlatDistance(currentTarget.transform.position) > companionModel.DetectRange))
        {
            currentTarget = null;
        }

        if (Time.time < nextTargetScanTime)
        {
            return;
        }

        nextTargetScanTime = Time.time + companionModel.TargetScanInterval;

        MonsterPresenter nearest = null;
        float nearestDistance = companionModel.DetectRange;

        foreach (MonsterPresenter monster in FindObjectsByType<MonsterPresenter>(FindObjectsSortMode.None))
        {
            if (monster.IsDead)
            {
                continue;
            }

            float distance = GetFlatDistance(monster.transform.position);

            if (distance <= nearestDistance)
            {
                nearest = monster;
                nearestDistance = distance;
            }
        }

        currentTarget = nearest;
    }

    public void FollowPlayer()
    {
        if (playerTransform != null)
        {
            MoveTo(playerTransform.position);
        }
    }

    public void ChaseTarget()
    {
        if (currentTarget != null)
        {
            MoveTo(currentTarget.transform.position);
        }
    }

    // NavMesh는 게임 시작 몇 초 뒤에 만들어지므로(RuntimeNavMeshBuilder), 처음에 못 올라탄 에이전트를 다시 올려줌
    private void TryPlaceOnNavMesh()
    {
        if (agent == null || agent.isOnNavMesh || Time.time < nextNavMeshPlaceTime)
        {
            return;
        }

        nextNavMeshPlaceTime = Time.time + 1f;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.enabled = false;
            agent.enabled = true;
        }
    }

    private void MoveTo(Vector3 destination)
    {
        TryPlaceOnNavMesh();

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = companionModel.MoveSpeed;
            agent.SetDestination(destination);
            return;
        }

        // NavMesh를 쓸 수 없을 때의 예비 이동(직선). 높이는 유지.
        destination.y = transform.position.y;

        LookAt(destination);
        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            companionModel.MoveSpeed * Time.deltaTime);
    }

    public void StopMove()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public void LookAtTarget()
    {
        if (currentTarget != null)
        {
            LookAt(currentTarget.transform.position);
        }
    }

    private void LookAt(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <summary>
    /// 공격 간격이 지났으면 현재 대상에게 피해를 줌. 간격은 상태를 나갔다 들어와도 유지됨.
    /// </summary>
    public void TryAttackTarget()
    {
        if (currentTarget == null || currentTarget.IsDead || Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + companionModel.AttackCooldown;
        currentTarget.TakeDamage(companionModel.AttackPower);
    }

    private float GetFlatDistance(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;

        return offset.magnitude;
    }
}
