public class CompanionChaseState : ICompanionState
{
    private CompanionPresenter companionPresenter;
    private CompanionStateManager stateManager;

    public CompanionChaseState(CompanionPresenter companionPresenter, CompanionStateManager stateManager)
    {
        this.companionPresenter = companionPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
    }

    public void Update()
    {
        stateManager.UpdateDecision();

        if (stateManager.CurrentStateId != CompanionStateId.Chase)
        {
            return;
        }

        companionPresenter.ChaseTarget();
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
        companionPresenter.StopMove();
    }
}
