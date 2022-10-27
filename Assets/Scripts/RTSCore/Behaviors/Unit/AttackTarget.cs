using Swordfish.Library.BehaviorTrees;

public class AttackTarget : BehaviorNode<UnitV2>
{
    public override BehaviorState Evaluate(UnitV2 unit, float delta)
    {
        unit.AttackingTarget = unit.Target.IsAlive();

        return unit.AttackingTarget ? BehaviorState.RUNNING : BehaviorState.SUCCESS;
    }
}
