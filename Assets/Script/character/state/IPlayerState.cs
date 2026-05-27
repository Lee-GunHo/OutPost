public interface IPlayerState
{
    void Enter(); // 상태 진입
    void Update(); // 매 프레임 판단
    void FixedUpdate(); // 물리 이동
    void Exit(); // 상태 종료
}