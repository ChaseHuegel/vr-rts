using Swordfish.Library.BehaviorTrees;

public class SetStateToGathering : BehaviorAction<VillagerV2>
{
    public override void Run(VillagerV2 villager, float delta)
    {
        switch (villager.CargoType)
        {
            case ResourceGatheringType.Grain:
                villager.State = ActorAnimationState.FARMING;
                break;

            case ResourceGatheringType.Berries:
            case ResourceGatheringType.Meat:
                villager.State = ActorAnimationState.FORAGING;
                break;

            case ResourceGatheringType.Fish:
                villager.State = ActorAnimationState.FISHING;
                break;

            case ResourceGatheringType.Gold:
            case ResourceGatheringType.Stone:
                villager.State = ActorAnimationState.MINING;
                break;

            case ResourceGatheringType.Wood:
                villager.State = ActorAnimationState.LUMBERJACKING;
                break;
        }
    }
}
