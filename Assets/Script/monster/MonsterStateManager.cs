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

    private void Awake()
    {
        monsterPresenter = GetComponent<MonsterPresenter>();

        IdleState = new MonsterIdleState(monsterPresenter, this);
        ChaseState = new MonsterChaseState(monsterPresenter, this);
        AttackState = new MonsterAttackState(monsterPresenter, this);
        HitState = new MonsterHitState(monsterPresenter, this);
        DeadState = new MonsterDeadState(monsterPresenter, this);
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

    public void ChangeState(IMonsterState nextState)
    {
        currentState?.Exit();
        currentState = nextState;
        currentState?.Enter();
    }
}