using UnityEngine;

/// <summary>
/// 전투 동료 FSM 매니저. PlayerStateManager/MonsterStateManager와 같은 구조(상태 보유 + ChangeState).
/// 어떤 상태로 갈지는 CompanionDecision(순수 함수)이 정하고, 각 상태는 UpdateDecision()만 호출.
/// </summary>
public class CompanionStateManager : MonoBehaviour
{
    private CompanionPresenter companionPresenter;
    private ICompanionState currentState;

    public CompanionIdleState IdleState { get; private set; }
    public CompanionFollowState FollowState { get; private set; }
    public CompanionChaseState ChaseState { get; private set; }
    public CompanionAttackState AttackState { get; private set; }

    public CompanionStateId CurrentStateId { get; private set; } = CompanionStateId.Idle;

    // 런타임에 AddComponent로 붙으면 이 컴포넌트의 Awake가 CompanionPresenter보다 먼저 불리므로 Start에서 준비
    private void Start()
    {
        companionPresenter = GetComponent<CompanionPresenter>();

        IdleState = new CompanionIdleState(companionPresenter, this);
        FollowState = new CompanionFollowState(companionPresenter, this);
        ChaseState = new CompanionChaseState(companionPresenter, this);
        AttackState = new CompanionAttackState(companionPresenter, this);

        ChangeState(CompanionStateId.Idle);
    }

    private void Update()
    {
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    public void UpdateDecision()
    {
        CompanionStateId next = CompanionDecision.Decide(
            CurrentStateId,
            companionPresenter.Sense(),
            companionPresenter.RuleValues);

        if (next != CurrentStateId)
        {
            ChangeState(next);
        }
    }

    public void ChangeState(CompanionStateId nextStateId)
    {
        ICompanionState nextState = GetState(nextStateId);

        if (currentState == nextState)
        {
            return;
        }

        currentState?.Exit();
        currentState = nextState;
        CurrentStateId = nextStateId;
        currentState.Enter();
    }

    private ICompanionState GetState(CompanionStateId stateId)
    {
        switch (stateId)
        {
            case CompanionStateId.Follow: return FollowState;
            case CompanionStateId.Chase: return ChaseState;
            case CompanionStateId.Attack: return AttackState;
            default: return IdleState;
        }
    }
}
