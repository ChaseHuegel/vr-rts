using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class GoToTarget : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 target, float delta)
    {
        Coord2D position = target.Target.GetDirectionalCoord(target.gridPosition);

        if (target.DistanceTo(position) <= target.InteractReach)
        {
            return BehaviorState.SUCCESS;
        }

        if (!target.HasValidPath() || target.HasTargetChanged())
        {
            PathManager.RequestPath(target, position.x, position.y, true);
            return BehaviorState.RUNNING;
        }

        return BehaviorState.RUNNING;
    }
}
