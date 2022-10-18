using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class SetUnitState : BehaviorAction<ActorV2>
{
    private readonly ActorAnimationState State;

    public SetUnitState(ActorAnimationState state)
    {
        State = state;
    }

    public override void Run(ActorV2 target, float delta)
    {
        target.State = State;
    }
}
