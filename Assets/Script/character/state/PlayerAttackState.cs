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

        Debug.Log("Attack State ÁøÀÔ");
    }

    public void Update()
    {
        attackTimer -= Time.deltaTime;

        if (!hasAttacked)
        {
            playerPresenter.Attack();
            hasAttacked = true;
        }

        if (attackTimer <= 0f)
        {
            playerPresenter.ReturnToIdleOrMove();
        }
    }

    public void FixedUpdate()
    {
        playerPresenter.StopMove();
    }

    public void Exit()
    {
    }
}