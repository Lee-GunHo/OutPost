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
        stateManager.UpdateCombatDecision();
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
