using Swordfish.Library.BehaviorTrees;

public class HealTarget : BehaviorNode<UnitV2>
{
    public override BehaviorState Evaluate(UnitV2 unit, float delta)
    {
        unit.HealingTarget = !unit.Target.Attributes.Get(AttributeConstants.HEALTH).IsMax();

        return unit.HealingTarget ? BehaviorState.RUNNING : BehaviorState.SUCCESS;
    }
}
