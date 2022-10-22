using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class CanDropOffAtTarget : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 villager, float delta)
    {
        return villager.Target is Structure structure
            && structure.IsSameFaction(villager.FactionID)
            && structure.CanDropOff(villager.CargoType);
    }
}
