using System.Diagnostics;

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
        currentCooldown = 0f;
    }

    public void Update()
    {
        if (!monsterPresenter.IsPlayerInAttackRange())
        {
            stateManager.ChangeState(stateManager.ChaseState);
            return;
        }

        currentCooldown -= UnityEngine.Time.deltaTime;

        if (currentCooldown <= 0f)
        {
            monsterPresenter.Attack();
            currentCooldown = attackCooldown;
        }
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
    }
}