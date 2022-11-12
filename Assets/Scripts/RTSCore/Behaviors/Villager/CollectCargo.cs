using Swordfish.Library.BehaviorTrees;

public class CollectCargo : BehaviorNode<VillagerV2>
{
    public override BehaviorState Evaluate(VillagerV2 villager, float delta)
    {
        if (villager.IsCargoFull())
            return BehaviorState.SUCCESS;

        villager.CollectingTarget = villager.HasValidTarget<Resource>();

        return villager.CollectingTarget ? BehaviorState.RUNNING : BehaviorState.FAILED;
    }
}
