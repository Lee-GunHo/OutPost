// Flee intention/configuration live in the Model; navigation is coordinated by the Presenter.
public sealed class MonsterFleeState : IMonsterState
{
    private readonly MonsterPresenter owner;
    private readonly MonsterStateManager states;

    public MonsterFleeState(MonsterPresenter owner, MonsterStateManager states)
    {
        this.owner = owner;
        this.states = states;
    }

    public void Enter() { owner.Behavior.BeginFleeMovement(); }
    public void Update()
    {
        if (!owner.Behavior.TickFlee()) states.UpdateCombatDecision();
    }
    public void FixedUpdate() { }
    // Being hit interrupts movement, but the intention survives until hit recovery ends.
    public void Exit() { owner.Behavior.PauseFleeMovement(); }
}
