public class CompanionFollowState : ICompanionState
{
    private CompanionPresenter companionPresenter;
    private CompanionStateManager stateManager;

    public CompanionFollowState(CompanionPresenter companionPresenter, CompanionStateManager stateManager)
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

        if (stateManager.CurrentStateId != CompanionStateId.Follow)
        {
            return;
        }

        companionPresenter.FollowPlayer();
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
        companionPresenter.StopMove();
    }
}
