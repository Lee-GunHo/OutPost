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

        attackTimer = 0f;
        hasAttacked = false;

    }

    public void Update()
    {
        if (!hasAttacked)
        {
            // The actual target determines whether combat or wall-break recovery applies.
            attackTimer = playerPresenter.ExecuteAttackAction();
            hasAttacked = true;
        }

        attackTimer -= Time.deltaTime;

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
