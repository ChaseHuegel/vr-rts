using Swordfish.Library.BehaviorTrees;
using UnityEngine;

public class HealTarget : BehaviorNode<UnitV2>
{
    public override BehaviorState Evaluate(UnitV2 unit, float delta)
    {
        // TODO: Is this needed here? Sick of messing with it, but
        // it works for now.   
        if (!unit.Target.IsAlive())
            unit.HealingTarget = false;
        else if (unit.Target.Attributes.Get(AttributeType.HEALTH).IsMax())
            unit.HealingTarget = false;
        else
            unit.HealingTarget = true;

        Debug.LogFormat("{0} : {1}/{2} : {3}", unit.Target.name,
            unit.Target.Attributes.ValueOf(AttributeType.HEALTH),
            unit.Target.Attributes.MaxValueOf(AttributeType.HEALTH),
            unit.Target.IsAlive());

        return unit.HealingTarget ? BehaviorState.RUNNING : BehaviorState.SUCCESS;
    }
}
