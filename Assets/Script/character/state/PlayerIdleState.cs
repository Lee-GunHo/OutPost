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
        if (playerPresenter.IsAttackPressed)
        {
            if (playerPresenter.IsPickaxeMode)
            {
                playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.MineState);
                return;
            }

            if (playerPresenter.IsWeaponMode)
            {
                playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.AttackState);
                return;
            }
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