using UnityEngine;

public class MonsterAttackState : IMonsterState
{
    private MonsterPresenter monsterPresenter;
    private MonsterStateManager stateManager;

    private float nextAttackTime = float.NegativeInfinity;

    public MonsterAttackState(MonsterPresenter monsterPresenter, MonsterStateManager stateManager)
    {
        this.monsterPresenter = monsterPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        monsterPresenter.StopMove();

        // Keep the attack deadline when returning from chase or hit states.
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

        monsterPresenter.StopMove();

        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + monsterPresenter.AttackCooldown;
        AttackOnce();
    }

    public void FixedUpdate()
    {
        monsterPresenter.StopMove();
    }

    public void Exit()
    {
    }

    private void AttackOnce()
    {
        Transform target = monsterPresenter.CurrentTarget;

        if (target == null)
        {
            Debug.LogWarning("공격 대상이 없습니다.");
            return;
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            damageable = target.GetComponentInChildren<IDamageable>();
        }

        if (damageable == null)
        {
            Debug.LogWarning("현재 타겟에게 IDamageable이 없습니다: " + target.name);
            return;
        }

        damageable.TakeDamage(monsterPresenter.AttackPower);

        Debug.Log(
            "몬스터 공격 성공 / 타겟: " + target.name +
            " / 데미지: " + monsterPresenter.AttackPower
        );

        monsterPresenter.TryApplyStatusEffectToCurrentTarget();
    }
}