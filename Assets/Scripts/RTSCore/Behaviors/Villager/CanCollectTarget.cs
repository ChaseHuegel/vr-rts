using Swordfish.Library.BehaviorTrees;

public class CanCollectTarget : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 villager, float delta)
    {
        return villager.Target is Resource resource && resource.yield > 0;
    }
}
