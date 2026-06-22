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
        Debug.Log("몬스터 Hit 상태 진입");

        monsterPresenter.StopMove();
        hitTimer = monsterPresenter.HitDuration;
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

        if (monsterPresenter.IsPlayerInAttackRange())
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
    }

    public void Exit()
    {
    }
}