using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class GoToDestination : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 target, float delta)
    {
        if (!target.HasValidPath() || target.DestinationChangedRecently)
        {
            if (target.GetDistanceTo(target.Destination.x, target.Destination.y) <= 1)
                return BehaviorState.SUCCESS;

            PathManager.RequestPath(target, target.Destination.x, target.Destination.y, true);
            return BehaviorState.RUNNING;
        }

        if (target.CurrentPath?.Count == 0)
        {
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.RUNNING;
    }
}
