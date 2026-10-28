// Screen state and copy. Navigation belongs to GameOverPresenter.
public sealed class GameOverModel
{
    public const string MainScene = "MainScene";
    public string Title => "거점 방어 실패";
    public string Message => "넥서스가 파괴되었습니다.\n메인 메뉴에서 이어하기 또는 새로하기를 선택하세요.";
    public bool IsLeaving { get; private set; }
    public bool TryLeave()
    {
        if (IsLeaving) return false;
        IsLeaving = true;
        return true;
    }
    public void CancelLeave() { IsLeaving = false; }
}
