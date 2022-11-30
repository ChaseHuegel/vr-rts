using Swordfish.Library.BehaviorTrees;
using UnityEngine;

public class HealTarget : BehaviorNode<UnitV2>
{
    public override BehaviorState Evaluate(UnitV2 unit, float delta)
    {
        unit.HealingTarget = !unit.Target.Attributes.Get(AttributeType.HEALTH).IsMax();
        unit.HealingTarget &= unit.Target.IsAlive();
        
        return unit.HealingTarget ? BehaviorState.RUNNING : BehaviorState.SUCCESS;
    }
}
