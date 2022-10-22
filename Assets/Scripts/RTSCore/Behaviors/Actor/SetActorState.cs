using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class SetActorState : BehaviorAction<ActorV2>
{
    private readonly ActorAnimationState State;

    public SetActorState(ActorAnimationState state)
    {
        State = state;
    }

    public override void Run(ActorV2 actor, float delta)
    {
        actor.State = State;
    }
}
