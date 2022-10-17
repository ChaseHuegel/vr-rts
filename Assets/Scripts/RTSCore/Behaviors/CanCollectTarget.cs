using Swordfish.Library.BehaviorTrees;

public class CanCollectTarget : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 target, float delta)
    {
        return target.Target is Resource resource && resource.amount > 0;
    }
}
