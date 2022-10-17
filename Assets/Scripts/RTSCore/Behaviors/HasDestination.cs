using Swordfish.Library.BehaviorTrees;

public class HasDestination : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 target, float delta)
    {
        return target.Destination != null;
    }
}
