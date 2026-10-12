public class CompanionIdleState : ICompanionState
{
    private CompanionPresenter companionPresenter;
    private CompanionStateManager stateManager;

    public CompanionIdleState(CompanionPresenter companionPresenter, CompanionStateManager stateManager)
    {
        this.companionPresenter = companionPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        companionPresenter.StopMove();
    }

    public void Update()
    {
        stateManager.UpdateDecision();
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
    }
}
