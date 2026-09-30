public class NPCDialogueEndedState : INPCDialogueState
{
    private readonly NPCTipDialoguePresenter presenter;

    public NPCDialogueEndedState(NPCTipDialoguePresenter presenter)
    {
        this.presenter = presenter;
    }

    public void Enter()
    {
        presenter.EnterEnded();
    }

    public void Exit()
    {
    }
}
