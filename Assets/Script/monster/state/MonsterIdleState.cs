using UnityEngine;

public class MonsterIdleState : IMonsterState
{
    private MonsterPresenter monsterPresenter;
    private MonsterStateManager stateManager;

    public MonsterIdleState(MonsterPresenter monsterPresenter, MonsterStateManager stateManager)
    {
        this.monsterPresenter = monsterPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        monsterPresenter.StopMove();
    }

    public void Update()
    {
        monsterPresenter.UpdateTarget();

        if (monsterPresenter.HasTarget)
        {
            stateManager.ChangeState(stateManager.ChaseState);
            return;
        }
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
    }
}