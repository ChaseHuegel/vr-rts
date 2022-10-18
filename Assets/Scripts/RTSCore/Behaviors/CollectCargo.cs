using Swordfish.Library.BehaviorTrees;

public class CollectCargo : BehaviorNode<VillagerV2>
{
    public override BehaviorState Evaluate(VillagerV2 target, float delta)
    {
        if (target.IsCargoFull)
            return BehaviorState.SUCCESS;

        if (target.Target is Resource resource)
        {
            target.CargoType = resource.type;
            target.Cargo += resource.TryRemove(1);

            return BehaviorState.RUNNING;
        }

        return BehaviorState.FAILED;
    }
}
