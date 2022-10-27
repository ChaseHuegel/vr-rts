using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public static class SoldierBehaviorTree
{
    public static BehaviorTree<ActorV2> Get()
    {
        return new BehaviorTree<ActorV2>(
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

                new OrderIs(UnitOrder.Attack,
                    new BehaviorSelector(
                        //  Attempt to chase and attack the target
                        new BehaviorSequence(
                            new HasTarget(),
                            new PursueTarget(),
                            new SetActorState(ActorAnimationState.ATTACKING),
                            new AttackTarget(),
                            new ResetTarget(),
                            new ResetOrder()
                        ),
                        //  Else order is complete
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                ),

                //  Attempt to attack nearby enemies
                new BehaviorSequence(
                    new BehaviorSelector(
                        new HasTarget(),
                        new TargetNearestEnemy()
                    ),
                    new PursueTarget(),
                    new SetActorState(ActorAnimationState.ATTACKING),
                    new AttackTarget(),
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
}