using UnityEngine;

public class PlayerAttackState : IPlayerState
{
    private PlayerPresenter playerPresenter;

    private float attackTimer;
    private bool hasAttacked;

    public PlayerAttackState(PlayerPresenter playerPresenter)
    {
        this.playerPresenter = playerPresenter;
    }

    public void Enter()
    {
        playerPresenter.StopMove();

        attackTimer = playerPresenter.AttackDuration;
        hasAttacked = false;

    }

    public void Update()
    {
        attackTimer -= Time.deltaTime;

        if (!hasAttacked)
        {
            playerPresenter.ExecuteAttackAction();
            hasAttacked = true;
        }

        if (attackTimer <= 0f)
        {
            playerPresenter.ReturnToIdleOrMove();
        }
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
    }
}