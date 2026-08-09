using UnityEngine;

public class BossStateManager : MonoBehaviour
{
    private IMonsterState currentState;

    private BossPresenter bossPresenter;
    private BossPatternController patternController;

    public BossIdleState IdleState { get; private set; }
    public BossPatternAttackState PatternAttackState { get; private set; }
    public BossDeadState DeadState { get; private set; }

    public BossPatternController PatternController => patternController;

    private void Awake()
    {
        bossPresenter = GetComponent<BossPresenter>();
        patternController = GetComponent<BossPatternController>();

        IdleState = new BossIdleState(bossPresenter, this);
        PatternAttackState = new BossPatternAttackState(bossPresenter, this);
        DeadState = new BossDeadState(bossPresenter, this);
    }

    private void Start()
    {
        ChangeState(IdleState);
    }

    private void Update()
    {
        currentState?.Update();
    }

    private void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }

    public void ChangeState(IMonsterState nextState)
    {
        if (nextState == null)
        {
            return;
        }

        currentState?.Exit();
        currentState = nextState;
        currentState.Enter();
    }
}