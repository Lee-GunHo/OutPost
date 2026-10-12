public class CompanionAttackState : ICompanionState
{
    private CompanionPresenter companionPresenter;
    private CompanionStateManager stateManager;

    public CompanionAttackState(CompanionPresenter companionPresenter, CompanionStateManager stateManager)
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

        if (stateManager.CurrentStateId != CompanionStateId.Attack)
        {
            return;
        }

        companionPresenter.LookAtTarget();
        companionPresenter.TryAttackTarget();
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
    }
}
