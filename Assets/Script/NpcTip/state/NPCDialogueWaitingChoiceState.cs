public class NPCDialogueWaitingChoiceState : INPCDialogueState
{
    private readonly NPCTipDialoguePresenter presenter;

    public NPCDialogueWaitingChoiceState(NPCTipDialoguePresenter presenter)
    {
        this.presenter = presenter;
    }

    public void Enter()
    {
        presenter.EnterWaitingChoice();
    }

    public void Exit()
    {
    }
}
