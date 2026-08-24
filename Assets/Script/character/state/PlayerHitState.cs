using UnityEngine;

public class PlayerHitState : IPlayerState
{
    private PlayerPresenter playerPresenter;

    private float hitTimer;

    public PlayerHitState(PlayerPresenter playerPresenter)
    {
        this.playerPresenter = playerPresenter;
    }

    public void Enter()
    {
        Debug.Log("Player Hit State 진입");

        playerPresenter.StopMove();
        hitTimer = playerPresenter.HitDuration;
        playerPresenter.PlayHitEffect(playerPresenter.HitDuration);
    }

    public void Update()
    {
        hitTimer -= Time.deltaTime;

        if (hitTimer > 0f)
        {
            return;
        }

        if (playerPresenter.IsDead)
        {
            playerPresenter.StateManager.ChangeState(playerPresenter.StateManager.DeadState);
            return;
        }

        playerPresenter.ReturnToIdleOrMove();
    }

    public void FixedUpdate()
    {
        playerPresenter.StopMove();
    }

    public void Exit()
    {
    }
}