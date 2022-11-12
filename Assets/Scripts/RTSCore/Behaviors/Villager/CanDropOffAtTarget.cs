using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class CanDropOffAtTarget : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 villager, float delta)
    {
        return villager.TryGetTarget(out Structure structure)
            && structure.Faction.IsSameFaction(villager.Faction)
            && structure.CanDropOff(villager.CargoType);
    }
}
