using UnityEngine;

public class MonsterHitState : IMonsterState
{
    private MonsterPresenter monsterPresenter;
    private MonsterStateManager stateManager;

    private float hitTimer;

    public MonsterHitState(MonsterPresenter monsterPresenter, MonsterStateManager stateManager)
    {
        this.monsterPresenter = monsterPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {

        monsterPresenter.StopMove();
        hitTimer = monsterPresenter.HitDuration;
        monsterPresenter.KnockbackFromPlayer();
    }

    public void Update()
    {
        hitTimer -= Time.deltaTime;

        if (hitTimer > 0f)
        {
            return;
        }

        if (monsterPresenter.IsDead)
        {
            stateManager.ChangeState(stateManager.DeadState);
            return;
        }

        if (monsterPresenter.IsCurrentTargetInAttackRange())
        {
            stateManager.ChangeState(stateManager.AttackState);
            return;
        }

        if (monsterPresenter.IsPlayerInChaseRange())
        {
            stateManager.ChangeState(stateManager.ChaseState);
            return;
        }

        stateManager.ChangeState(stateManager.IdleState);
    }

    public void FixedUpdate()
    {
        monsterPresenter.UpdateKnockback(Time.fixedDeltaTime);
    }

    public void Exit()
    {
        monsterPresenter.StopMove();
    }
}
