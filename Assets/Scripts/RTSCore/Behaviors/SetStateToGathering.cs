using Swordfish.Library.BehaviorTrees;

public class SetStateToGathering : BehaviorAction<VillagerV2>
{
    public override void Run(VillagerV2 target, float delta)
    {
        switch (target.CargoType)
        {
            case ResourceGatheringType.Grain:
                target.State = ActorAnimationState.FARMING;
                break;

            case ResourceGatheringType.Berries:
            case ResourceGatheringType.Meat:
                target.State = ActorAnimationState.FORAGING;
                break;

            case ResourceGatheringType.Fish:
                target.State = ActorAnimationState.FISHING;
                break;

            case ResourceGatheringType.Gold:
            case ResourceGatheringType.Stone:
                target.State = ActorAnimationState.MINING;
                break;

            case ResourceGatheringType.Wood:
                target.State = ActorAnimationState.LUMBERJACKING;
                break;
        }
    }
}
