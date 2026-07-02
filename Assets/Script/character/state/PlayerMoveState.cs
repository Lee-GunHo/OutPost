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
        if (playerPresenter.IsAttackPressed)
        {
            playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.AttackState);
            return;
        }

        if (playerPresenter.IsDashPressed && playerPresenter.CanDash)
        {
            playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.DashState);
            return;
        }

        if (playerPresenter.IsInteractPressed)
        {
            playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.InteractState);
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