using Swordfish.Library.BehaviorTrees;

public class IsCargoFull : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 villager, float delta)
    {
        return villager.IsCargoFull;
    }
}
