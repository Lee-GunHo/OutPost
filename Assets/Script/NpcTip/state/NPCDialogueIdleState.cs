public class NPCDialogueIdleState : INPCDialogueState
{
    private readonly NPCTipDialoguePresenter presenter;

    public NPCDialogueIdleState(NPCTipDialoguePresenter presenter)
    {
        this.presenter = presenter;
    }

    public void Enter()
    {
        presenter.EnterIdle();
    }

    public void Exit()
    {
    }
}
