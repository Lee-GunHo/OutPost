public class MonsterIdleState : IMonsterState
{
    private MonsterPresenter monsterPresenter;
    private MonsterStateManager stateManager;

    public MonsterIdleState(MonsterPresenter monsterPresenter, MonsterStateManager stateManager)
    {
        this.monsterPresenter = monsterPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        monsterPresenter.StopMove();
    }

    public void Update()
    {
        if (monsterPresenter.IsPlayerInChaseRange())
        {
            stateManager.ChangeState(stateManager.ChaseState);
        }
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
    }
}