public class MonsterChaseState : IMonsterState
{
    private MonsterPresenter monsterPresenter;
    private MonsterStateManager stateManager;

    public MonsterChaseState(MonsterPresenter monsterPresenter, MonsterStateManager stateManager)
    {
        this.monsterPresenter = monsterPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
    }

    public void Update()
    {
        if (monsterPresenter.IsPlayerInAttackRange())
        {
            stateManager.ChangeState(stateManager.AttackState);
            return;
        }

        if (!monsterPresenter.IsPlayerInChaseRange())
        {
            stateManager.ChangeState(stateManager.IdleState);
            return;
        }
    }

    public void FixedUpdate()
    {
        monsterPresenter.ChasePlayer();
    }

    public void Exit()
    {
        monsterPresenter.StopMove();
    }
}