using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public static class FaunaBehaviorTree
{
    public static BehaviorTree<ActorV2> Get()
    {
        return new BehaviorTree<ActorV2>(
            new BehaviorSelector(

                //  Try to navigate to our current destination
                new IfHasDestination(
                    new BehaviorSequence(
                        new GoToDestination(),
                        new ResetDestination()
                    )
                ),

                //  Try to navigate to our current target
                new IfHasTarget(
                    new BehaviorSequence(
                        new GoToTarget(),
                        new ResetTarget()
                    )
                ),

                new SetActorState(ActorAnimationState.IDLE)
            )
        );
    }
}