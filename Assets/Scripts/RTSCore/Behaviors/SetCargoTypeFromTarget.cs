using Swordfish.Library.BehaviorTrees;

public class SetCargoTypeFromTarget : BehaviorNode<VillagerV2>
{
    public override BehaviorState Evaluate(VillagerV2 target, float delta)
    {
        if (target.Target is Resource resource)
        {
            target.CargoType = resource.type;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
