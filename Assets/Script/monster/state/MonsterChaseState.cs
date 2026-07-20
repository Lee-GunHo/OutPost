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
        monsterPresenter.UpdateTarget();

        if (!monsterPresenter.HasTarget)
        {
            stateManager.ChangeState(stateManager.IdleState);
            return;
        }

        if (monsterPresenter.IsCurrentTargetInAttackRange())
        {
            stateManager.ChangeState(stateManager.AttackState);
            return;
        }
    }

    public void FixedUpdate()
    {
        monsterPresenter.ChaseTarget();
    }

    public void Exit()
    {
        monsterPresenter.StopMove();
    }
}