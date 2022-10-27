using Swordfish.Library.BehaviorTrees;

public class AttackTarget : BehaviorNode<UnitV2>
{
    public override BehaviorState Evaluate(UnitV2 unit, float delta)
    {
        unit.Attacking = unit.Target.IsAlive();

        return unit.Attacking ? BehaviorState.RUNNING : BehaviorState.SUCCESS;
    }
}
