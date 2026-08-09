using System.Collections;
using UnityEngine;

public class BossPatternAttackState : IMonsterState
{
    private BossPresenter bossPresenter;
    private BossStateManager stateManager;

    private Coroutine attackCoroutine;

    public BossPatternAttackState(BossPresenter bossPresenter, BossStateManager stateManager)
    {
        this.bossPresenter = bossPresenter;
        this.stateManager = stateManager;
    }

    public void Enter()
    {
        bossPresenter.StopMove();
        attackCoroutine = bossPresenter.StartCoroutine(AttackRoutine());
    }

    public void Update()
    {
    }

    public void FixedUpdate()
    {
        bossPresenter.StopMove();
    }

    public void Exit()
    {
        if (attackCoroutine != null)
        {
            bossPresenter.StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    private IEnumerator AttackRoutine()
    {
        BossPatternBase pattern = null;

        if (stateManager.PatternController != null)
        {
            pattern = stateManager.PatternController.GetAvailablePattern(bossPresenter);
        }

        if (pattern == null)
        {
            yield return new WaitForSeconds(0.2f);
            ReturnToIdle();
            yield break;
        }

        yield return pattern.RunPattern(bossPresenter);

        ReturnToIdle();
    }

    private void ReturnToIdle()
    {
        attackCoroutine = null;

        if (bossPresenter.IsDead)
        {
            stateManager.ChangeState(stateManager.DeadState);
            return;
        }

        stateManager.ChangeState(stateManager.IdleState);
    }
}