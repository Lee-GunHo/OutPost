public class BossDeadState : IMonsterState
{
    private BossPresenter bossPresenter;

    public BossDeadState(BossPresenter bossPresenter, BossStateManager stateManager)
    {
        this.bossPresenter = bossPresenter;
    }

    public void Enter()
    {
        bossPresenter.Dead();
    }

    public void Update()
    {
    }

    public void FixedUpdate()
    {
        bossPresenter.StopMove();
    }

    public void Exit()
    {
    }
}