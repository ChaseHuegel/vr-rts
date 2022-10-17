using Swordfish.Library.BehaviorTrees;

public class IsCargoFull : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 target, float delta)
    {
        return target.IsCargoFull;
    }
}
