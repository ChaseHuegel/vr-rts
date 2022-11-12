using Swordfish.Library.BehaviorTrees;

public class SetCargoTypeFromTarget : BehaviorNode<VillagerV2>
{
    public override BehaviorState Evaluate(VillagerV2 villager, float delta)
    {
        if (villager.TryGetTarget(out Resource resource))
        {
            villager.CargoType = resource.type;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
