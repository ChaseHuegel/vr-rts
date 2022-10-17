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

            switch (resource.type)
            {
                case ResourceGatheringType.Grain:
                    target.Animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.FARMING);
                    break;

                case ResourceGatheringType.Berries:
                case ResourceGatheringType.Meat:
                    target.Animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.FORAGING);
                    break;

                case ResourceGatheringType.Fish:
                    target.Animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.FISHING);
                    break;

                case ResourceGatheringType.Gold:
                case ResourceGatheringType.Stone:
                    target.Animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.MINING);
                    break;

                case ResourceGatheringType.Wood:
                    target.Animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.LUMBERJACKING);
                    break;
            }

            return BehaviorState.RUNNING;
        }

        return BehaviorState.FAILED;
    }
}
