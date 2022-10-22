using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class GoToDestination : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 actor, float delta)
    {
        if (!actor.HasValidPath() || actor.DestinationChangedRecently)
        {
            if (actor.GetDistanceTo(actor.Destination.x, actor.Destination.y) <= 1)
                return BehaviorState.SUCCESS;

            PathManager.RequestPath(actor, actor.Destination.x, actor.Destination.y, true);
            return BehaviorState.RUNNING;
        }

        if (actor.CurrentPath?.Count == 0)
        {
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.RUNNING;
    }
}
