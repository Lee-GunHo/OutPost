/// <summary>
/// F키 안내 UI(InteractionPromptUI)에 보여줄 정보.
/// IInteractable과 같이 구현하면 안내에 이름/행동 문구가 표시되고,
/// CanInteract가 false인 동안은 상호작용 대상(외곽선/안내/F키)으로 잡히지 않음.
/// </summary>
public interface IInteractionPromptSource
{
    // 플레이어가 상호작용 범위(트리거) 안에 있어 지금 F키가 먹히는지
    bool CanInteract { get; }

    // 안내 첫 줄에 표시할 대상 이름 (예: 상자, 나무 제작대, NPC 이름)
    string InteractionDisplayName { get; }

    // "F 눌러서 ○○"의 ○○ 부분 (예: 열기, 사용하기, 대화하기)
    string InteractionActionText { get; }
}
