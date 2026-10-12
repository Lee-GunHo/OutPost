public enum CompanionStateId
{
    Idle,
    Follow,
    Chase,
    Attack
}

/// <summary>
/// 상태 판단에 쓰는 현재 상황. CompanionPresenter가 매 프레임 채워서 넘김.
/// </summary>
public struct CompanionSense
{
    public bool IsRecruited;        // 플레이어가 전투 NPC를 발견했는가
    public float DistanceToPlayer;
    public bool HasTarget;          // 탐지 범위 안에 살아있는 몬스터가 있는가
    public float DistanceToTarget;
}

/// <summary>
/// 상태 판단 기준값. CompanionModel의 인스펙터 값에서 만들어짐.
/// </summary>
public struct CompanionRuleValues
{
    public float FollowStartDistance;
    public float FollowStopDistance;
    public float AttackRange;
    public float LeashDistance;
}

/// <summary>
/// 전투 동료의 상태 분류 기준(순수 함수). 가중치/점수 없이 위에서부터 처음 맞는 규칙 하나로 결정.
///
/// 1. 미합류(전투 NPC 미발견)                          → Idle
/// 2. 플레이어와 LeashDistance 이상 떨어짐              → Follow (전투 포기하고 복귀)
/// 3. 복귀(Follow) 중이고 아직 FollowStartDistance 밖   → Follow (곁에 붙을 때까지 전투 무시)
/// 4. 대상 몬스터가 AttackRange 안                      → Attack
/// 5. 대상 몬스터가 있음                                → Chase
/// 6. Follow 중이고 FollowStopDistance보다 멂           → Follow
/// 7. Follow 중이 아니고 FollowStartDistance보다 멂     → Follow
/// 8. 그 외                                             → Idle
/// </summary>
public static class CompanionDecision
{
    public static CompanionStateId Decide(
        CompanionStateId current,
        CompanionSense sense,
        CompanionRuleValues rules)
    {
        if (!sense.IsRecruited)
        {
            return CompanionStateId.Idle;
        }

        if (sense.DistanceToPlayer >= rules.LeashDistance)
        {
            return CompanionStateId.Follow;
        }

        if (current == CompanionStateId.Follow && sense.DistanceToPlayer > rules.FollowStartDistance)
        {
            return CompanionStateId.Follow;
        }

        if (sense.HasTarget)
        {
            return sense.DistanceToTarget <= rules.AttackRange
                ? CompanionStateId.Attack
                : CompanionStateId.Chase;
        }

        // 시작/정지 거리를 따로 둬서 경계에서 Idle↔Follow가 매 프레임 바뀌지 않게 함
        float followThreshold = current == CompanionStateId.Follow
            ? rules.FollowStopDistance
            : rules.FollowStartDistance;

        return sense.DistanceToPlayer > followThreshold
            ? CompanionStateId.Follow
            : CompanionStateId.Idle;
    }
}
