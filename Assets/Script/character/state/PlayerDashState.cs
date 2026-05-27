using UnityEngine;

public class PlayerDashState : IPlayerState
{
    private PlayerPresenter playerPresenter;

    private float dashTimer;
    private Vector3 dashDirection;

    public PlayerDashState(PlayerPresenter playerPresenter)
    {
        this.playerPresenter = playerPresenter;
    }

    public void Enter()
    {
        dashTimer = playerPresenter.DashDuration;
        dashDirection = playerPresenter.GetDashDirection();

        playerPresenter.PlayDashBlinkEffect(playerPresenter.DashDuration);
    }

    public void Update()
    {
        dashTimer -= Time.deltaTime;

        if (dashTimer <= 0f)
        {
            playerPresenter.ReturnToIdleOrMove();
        }
    }

    public void FixedUpdate()
    {
        playerPresenter.DashMove(dashDirection);
    }

    public void Exit()
    {
        playerPresenter.StopMove();
    }
}