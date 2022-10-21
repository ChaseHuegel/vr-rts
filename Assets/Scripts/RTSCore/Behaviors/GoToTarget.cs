using System.Collections.Generic;
using System.Linq;
using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class GoToTarget : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 target, float delta)
    {
        Coord2D position = target.Target.GetNearestPositionFrom(target.GetPosition());

        if (target.GetDistanceTo(position.x, position.y) <= target.Reach)
        {
            target.ResetPath();
            return BehaviorState.SUCCESS;
        }

        if (!target.HasValidPath() || target.TargetChangedRecently)
        {
            PathManager.RequestPath(target, position.x, position.y, true);
            return BehaviorState.RUNNING;
        }

        return BehaviorState.RUNNING;
    }
}
