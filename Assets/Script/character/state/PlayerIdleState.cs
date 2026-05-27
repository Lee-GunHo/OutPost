using UnityEngine;

public class PlayerIdleState : IPlayerState
{
    private PlayerPresenter playerPresenter;

    public PlayerIdleState(PlayerPresenter playerPresenter)
    {
        this.playerPresenter = playerPresenter;
    }

    public void Enter()
    {
        playerPresenter.StopMove();
    }

    public void Update()
    {
        if (playerPresenter.IsDashPressed)
        {
            playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.DashState);
            return;
        }

        if (playerPresenter.MoveInput != Vector2.zero)
        {
            playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.MoveState);
        }
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
    }
}