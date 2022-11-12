using Swordfish.Library.BehaviorTrees;

public class CanCollectTarget : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 villager, float delta)
    {
        return villager.TryGetTarget(out Resource resource) && resource.amount > 0;
    }
}
