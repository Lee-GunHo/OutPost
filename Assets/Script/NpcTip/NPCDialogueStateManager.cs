using UnityEngine;

/// <summary>
/// NPCTipDialoguePresenter와 같은 GameObject에 붙는 FSM 매니저.
/// PlayerStateManager/MonsterStateManager와 동일한 구조(상태 보유 + ChangeState).
/// </summary>
public class NPCDialogueStateManager : MonoBehaviour
{
    private INPCDialogueState currentState;

    public INPCDialogueState IdleState { get; private set; }
    public INPCDialogueState ShowingLineState { get; private set; }
    public INPCDialogueState WaitingChoiceState { get; private set; }
    public INPCDialogueState EndedState { get; private set; }

    private void Awake()
    {
        NPCTipDialoguePresenter presenter = GetComponent<NPCTipDialoguePresenter>();

        IdleState = new NPCDialogueIdleState(presenter);
        ShowingLineState = new NPCDialogueShowingLineState(presenter);
        WaitingChoiceState = new NPCDialogueWaitingChoiceState(presenter);
        EndedState = new NPCDialogueEndedState(presenter);
    }

    private void Start()
    {
        ChangeState(IdleState);
    }

    public void ChangeState(INPCDialogueState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        currentState?.Exit();
        currentState = nextState;
        currentState?.Enter();
    }
}
