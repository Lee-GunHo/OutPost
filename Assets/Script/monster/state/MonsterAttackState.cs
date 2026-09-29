using UnityEngine;

public class MonsterAttackState : IMonsterState
{
    private readonly MonsterPresenter monsterPresenter;
    private readonly MonsterStateManager stateManager;

    public MonsterAttackState(MonsterPresenter monsterPresenter, MonsterStateManager stateManager)
    {
        this.monsterPresenter = monsterPresenter;
        this.stateManager = stateManager;
    }

    public void Enter() { monsterPresenter.StopMove(); }

    public void Update()
    {
        monsterPresenter.UpdateTarget();
        MonsterCombatPresenter combat = monsterPresenter.Combat;
        if (combat.IsExecuting)
        {
            combat.Tick(Time.deltaTime);
            return;
        }
        if (!monsterPresenter.HasTarget)
        {
            stateManager.ChangeState(stateManager.IdleState);
            return;
        }
        if (!combat.TryStartSelectedAttack())
            stateManager.ChangeState(stateManager.ChaseState);
    }

    public void FixedUpdate() { monsterPresenter.StopMove(); }
    public void Exit() { monsterPresenter.Combat.Cancel(); }
}
