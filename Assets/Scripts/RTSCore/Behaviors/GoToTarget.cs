using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class GoToTarget : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 target, float delta)
    {
        if (target.DistanceTo(target.Target.gridPosition.x, target.Target.gridPosition.y) <= target.InteractReach)
            return BehaviorState.SUCCESS;

        if (!target.HasValidPath() || target.HasTargetChanged())
        {
            PathManager.RequestPath(target, target.Target.gridPosition.x, target.Target.gridPosition.y, true);
            return BehaviorState.RUNNING;
        }

        return BehaviorState.RUNNING;
    }
}
