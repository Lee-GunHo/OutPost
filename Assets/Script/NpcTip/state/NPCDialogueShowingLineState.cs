public class NPCDialogueShowingLineState : INPCDialogueState
{
    private readonly NPCTipDialoguePresenter presenter;

    public NPCDialogueShowingLineState(NPCTipDialoguePresenter presenter)
    {
        this.presenter = presenter;
    }

    public void Enter()
    {
        presenter.EnterShowingLine();
    }

    public void Exit()
    {
    }
}
