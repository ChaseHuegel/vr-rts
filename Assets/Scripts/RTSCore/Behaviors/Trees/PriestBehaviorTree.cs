using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public static class PriestBehaviorTree
{
    public static readonly BehaviorTree<ActorV2> Value = new(
        new BehaviorSelector(
            new OrderIs(UnitOrder.GoTo,
                new BehaviorSelector(
                    //  Attempt to go to
                    new BehaviorSequence(
                        new HasDestination(),
                        new GoToDestination(),
                        new ResetDestination(),
                        new ResetOrder()
                    ),
                    //  Else order is complete
                    new BehaviorSequence(
                        new ResetDestination(),
                        new ResetOrder()
                    )
                )
            ),
        
            // //  Attempt to heal nearby allies
            new BehaviorSequence(
                new BehaviorSelector(
                    new IfHasTarget(
                        new TargetIsFriendly()
                    ),
                    new TargetNearestFriendly()
                ),
                new PursueTarget(),
                new SetActorState(ActorAnimationState.HEAL),
                new HealTarget(),
                new ResetTarget()
            ),

            //  Try to navigate to our current destination
            new IfHasDestination(
                new BehaviorSequence(
                    new GoToDestination(),
                    new ResetDestination()
                )
            ),

            new SetActorState(ActorAnimationState.IDLE)
        )
    );
}