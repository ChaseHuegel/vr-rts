using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class CanDropOffAtTarget : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 target, float delta)
    {
        return target.Target is Structure structure
            && structure.IsSameFaction(target.factionId)
            && structure.CanDropOff(target.CargoType);
    }
}
