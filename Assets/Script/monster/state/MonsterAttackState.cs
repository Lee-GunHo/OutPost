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
        if (!stateManager.UpdateCombatDecision()) return;
        if (!combat.TryStartSelectedAttack())
            stateManager.ChangeState(stateManager.ChaseState);
    }

    public void FixedUpdate() { monsterPresenter.Combat.FixedTick(Time.fixedDeltaTime); }
    public void Exit() { monsterPresenter.Combat.Cancel(); }
}
