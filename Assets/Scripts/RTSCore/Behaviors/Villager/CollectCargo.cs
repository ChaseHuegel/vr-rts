using Swordfish.Library.BehaviorTrees;

public class CollectCargo : BehaviorNode<VillagerV2>
{
    public override BehaviorState Evaluate(VillagerV2 villager, float delta)
    {
        if (villager.IsCargoFull())
            return BehaviorState.SUCCESS;

        if (villager.Target is Resource resource)
        {
            villager.Attributes.Get(AttributeConstants.CARGO).Add(resource.TryRemove(1));
            return BehaviorState.RUNNING;
        }

        return BehaviorState.FAILED;
    }
}
