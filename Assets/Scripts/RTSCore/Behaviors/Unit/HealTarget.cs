using Swordfish.Library.BehaviorTrees;
using UnityEngine;

public class HealTarget : BehaviorNode<UnitV2>
{
    public override BehaviorState Evaluate(UnitV2 unit, float delta)
    {
        unit.HealingTarget = !unit.Target.Attributes.Get(AttributeType.HEALTH).IsMax();

        // Debug.Log(unit.Target.Attributes.ValueOf(AttributeType.HEALTH));
        // Debug.Log(unit.Target.Attributes.MaxValueOf(AttributeType.HEALTH));

        // Debug.Log(unit.Target.Attributes.Get(AttributeType.HEALTH).MaxValue.ToString());
        // Debug.Log(unit.Target.Attributes.Get(AttributeType.HEALTH).Value.ToString());

        Debug.LogFormat("{0} : {1}/{2}", unit.Target.name,
            unit.Target.Attributes.ValueOf(AttributeType.HEALTH),
            unit.Target.Attributes.MaxValueOf(AttributeType.HEALTH));

        return unit.HealingTarget ? BehaviorState.RUNNING : BehaviorState.SUCCESS;
    }
}
