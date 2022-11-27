using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class PursueTarget : BehaviorNode<UnitV2>
{
    public override BehaviorState Evaluate(UnitV2 unit, float delta)
    {
        Coord2D position = unit.Target.GetNearestPositionFrom(unit.GetPosition());

        if (unit.GetDistanceTo(position.x, position.y) <= unit.Attributes.ValueOf(AttributeType.ATTACK_RANGE))
        {
            unit.ResetPath();
            return BehaviorState.SUCCESS;
        }

        if (!unit.HasValidPath() || unit.TargetChangedRecently || (unit.Target is ActorV2 target && target.PositionChangedRecently))
        {
            PathManager.RequestPath(unit, position.x, position.y, true);
            return BehaviorState.RUNNING;
        }

        return BehaviorState.RUNNING;
    }
}
