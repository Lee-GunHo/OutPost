/// <summary>
/// NPC 팁 대화 상태. Player/Monster FSM과 같은 모양(Enter/Exit)을 따름.
/// 대화는 물리 갱신이 필요 없어 FixedUpdate/Update는 두지 않음.
/// </summary>
public interface INPCDialogueState
{
    void Enter();
    void Exit();
}
