public class BossIdleState : IMonsterState
{
    private BossPresenter bossPresenter;
    private BossStateManager stateManager;

    public BossIdleState(BossPresenter bossPresenter, BossStateManager stateManager)
    {
        this.bossPresenter = bossPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        bossPresenter.StopMove();
    }

    public void Update()
    {
        if (bossPresenter.IsDead)
        {
            stateManager.ChangeState(stateManager.DeadState);
            return;
        }

        if (!bossPresenter.HasTarget)
        {
            return;
        }

        if (!bossPresenter.IsTargetInDetectionRange())
        {
            return;
        }

        if (stateManager.PatternController != null &&
            stateManager.PatternController.HasAvailablePattern(bossPresenter))
        {
            stateManager.ChangeState(stateManager.PatternAttackState);
        }
    }

    public void FixedUpdate()
    {
        bossPresenter.StopMove();
    }

    public void Exit()
    {
    }
}