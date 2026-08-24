using UnityEngine;

public class MonsterDeadState : IMonsterState
{
    private MonsterPresenter monsterPresenter;
    private MonsterStateManager stateManager;

    public MonsterDeadState(MonsterPresenter monsterPresenter, MonsterStateManager stateManager)
    {
        this.monsterPresenter = monsterPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        Debug.Log("몬스터 Dead 상태 진입");

        monsterPresenter.Dead();
    }

    public void Update()
    {
    }

    public void FixedUpdate()
    {
    }

    public void Exit()
    {
    }
}