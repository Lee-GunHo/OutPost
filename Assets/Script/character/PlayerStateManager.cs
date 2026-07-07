using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    private IPlayerState currentState;

    public IPlayerState IdleState { get; private set; }
    public IPlayerState MoveState { get; private set; }
    public IPlayerState DashState { get; private set; }
    public IPlayerState InteractState { get; private set; }
    public IPlayerState AttackState { get; private set; }
    public IPlayerState MineState { get; private set; }
    public IPlayerState HitState { get; private set; }
    public IPlayerState DeadState { get; private set; }

    private PlayerPresenter playerPresenter;

    private void Awake()
    {
        playerPresenter = GetComponent<PlayerPresenter>();

        IdleState = new PlayerIdleState(playerPresenter);
        MoveState = new PlayerMoveState(playerPresenter);
        DashState = new PlayerDashState(playerPresenter);
        InteractState = new PlayerInteractState(playerPresenter);
        AttackState = new PlayerAttackState(playerPresenter);
        MineState = new PlayerMineState(playerPresenter);
        HitState = new PlayerHitState(playerPresenter);
        DeadState = new PlayerDeadState(playerPresenter);
    }

    private void Start()
    {
        ChangeState(IdleState);
    }

    private void Update()
    {
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    public void ChangeState(IPlayerState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        Debug.Log("Player State Change: " + currentState + " -> " + newState);


        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }
}