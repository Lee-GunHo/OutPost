using UnityEngine;

public class PlayerMoveState : IPlayerState
{
    private PlayerPresenter playerPresenter;

    public PlayerMoveState(PlayerPresenter playerPresenter)
    {
        this.playerPresenter = playerPresenter;
    }

    public void Enter()
    {
    }

    public void Update()
    {
        if (playerPresenter.IsDashPressed)
        {
            playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.DashState);
            return;
        }

        if (playerPresenter.MoveInput == Vector2.zero)
        {
            playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.IdleState);
        }
    }

    public void FixedUpdate()
    {
        playerPresenter.Move();
    }

    public void Exit()
    {
        playerPresenter.StopMove();
    }
}