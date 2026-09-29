// FSM adapter; retreat decisions/data live in the behavior MVP components.
public sealed class MonsterRetreatState : IMonsterState
{
    private readonly MonsterPresenter owner;
    private readonly MonsterStateManager states;

    public MonsterRetreatState(MonsterPresenter owner, MonsterStateManager states)
    {
        this.owner = owner;
        this.states = states;
    }

    public void Enter() { owner.Behavior.BeginRetreat(); }
    public void Update()
    {
        if (!owner.Behavior.TickRetreat())
        {
            owner.Behavior.EndRetreat();
            states.UpdateCombatDecision();
        }
    }
    public void FixedUpdate() { }
    public void Exit() { owner.Behavior.EndRetreat(); }
}
