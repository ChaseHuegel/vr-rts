using System.Collections.Generic;
using System.Linq;
using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class GoToTarget : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 actor, float delta)
    {
        Coord2D position = actor.Target.GetNearestPositionFrom(actor.GetPosition());

        if (actor.GetDistanceTo(position.x, position.y) <= actor.Reach)
        {
            actor.ResetPath();
            return BehaviorState.SUCCESS;
        }

        if (!actor.HasValidPath() || actor.TargetChangedRecently)
        {
            PathManager.RequestPath(actor, position.x, position.y, true);
            return BehaviorState.RUNNING;
        }

        return BehaviorState.RUNNING;
    }
}
