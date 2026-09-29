using UnityEngine;

public class MonsterStateManager : MonoBehaviour
{
    private MonsterPresenter monsterPresenter;
    private IMonsterState currentState;

    public MonsterIdleState IdleState { get; private set; }
    public MonsterChaseState ChaseState { get; private set; }
    public MonsterAttackState AttackState { get; private set; }
    public MonsterHitState HitState { get; private set; }
    public MonsterDeadState DeadState { get; private set; }
    public MonsterRetreatState RetreatState { get; private set; }
    public MonsterFormationState FormationState { get; private set; }
    public MonsterFleeState FleeState { get; private set; }

    private void Awake()
    {
        monsterPresenter = GetComponent<MonsterPresenter>();

        IdleState = new MonsterIdleState(monsterPresenter, this);
        ChaseState = new MonsterChaseState(monsterPresenter, this);
        AttackState = new MonsterAttackState(monsterPresenter, this);
        HitState = new MonsterHitState(monsterPresenter, this);
        DeadState = new MonsterDeadState(monsterPresenter, this);
        RetreatState = new MonsterRetreatState(monsterPresenter, this);
        FormationState = new MonsterFormationState(monsterPresenter, this);
        FleeState = new MonsterFleeState(monsterPresenter, this);
    }

    private void Start()
    {
        ChangeState(IdleState);
    }

    private void Update()
    {
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    // Called only at decision boundaries, never during attack windup/recovery or hit stun.
    public bool UpdateCombatDecision()
    {
        IMonsterState next;
        if (monsterPresenter.IsDead) next = DeadState;
        else
        {
            switch (monsterPresenter.Behavior.Decide())
            {
                case MonsterBehaviorAction.Attack: next = AttackState; break;
                case MonsterBehaviorAction.Chase: next = ChaseState; break;
                case MonsterBehaviorAction.Retreat: next = RetreatState; break;
                case MonsterBehaviorAction.Formation: next = FormationState; break;
                case MonsterBehaviorAction.Flee: next = FleeState; break;
                default: next = IdleState; break;
            }
        }
        if (currentState != next) ChangeState(next);
        return next == AttackState;
    }

    public void ChangeState(IMonsterState nextState)
    {
        currentState?.Exit();
        currentState = nextState;
        currentState?.Enter();
    }
}
