using UnityEngine;

public class MonsterAttackState : IMonsterState
{
    private MonsterPresenter monsterPresenter;
    private MonsterStateManager stateManager;

    private float attackCooldown = 1f;
    private float currentCooldown;

    public MonsterAttackState(MonsterPresenter monsterPresenter, MonsterStateManager stateManager)
    {
        this.monsterPresenter = monsterPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        monsterPresenter.StopMove();

        // 공격 상태에 들어오자마자 바로 공격 가능
        currentCooldown = 0f;
    }

    public void Update()
    {
        monsterPresenter.UpdateTarget();

        if (!monsterPresenter.HasTarget)
        {
            stateManager.ChangeState(stateManager.IdleState);
            return;
        }

        if (!monsterPresenter.IsCurrentTargetInAttackRange())
        {
            stateManager.ChangeState(stateManager.ChaseState);
            return;
        }

        currentCooldown -= Time.deltaTime;

        if (currentCooldown <= 0f)
        {
            monsterPresenter.Attack();
            currentCooldown = attackCooldown;
        }
    }

    public void FixedUpdate()
    {
        monsterPresenter.StopMove();
    }

    public void Exit()
    {
    }
}