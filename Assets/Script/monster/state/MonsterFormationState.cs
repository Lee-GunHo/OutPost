// Movement and role coordination are handled by the behavior presenter.
public sealed class MonsterFormationState : IMonsterState
{
    private readonly MonsterPresenter owner;
    private readonly MonsterStateManager states;
    public MonsterFormationState(MonsterPresenter owner, MonsterStateManager states)
    {
        this.owner = owner;
        this.states = states;
    }
    public void Enter() { owner.Behavior.BeginFormation(); }
    public void Update() { states.UpdateCombatDecision(); }
    public void FixedUpdate() { owner.Behavior.MoveFormation(); }
    public void Exit() { owner.StopMove(); }
}
