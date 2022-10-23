using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public static class VillagerBehaviorTree
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

                new OrderIs(UnitOrder.Collect,
                    new BehaviorSelector(
                        //  Attempt to drop off if cargo is full
                        new BehaviorSequence(
                            new IsCargoFull(),
                            new BehaviorSelector(
                                //  Try the current target
                                new CanDropOffAtTarget(),
                                //  Or get the nearest dropoff
                                new TargetNearestDropOff()
                            ),
                            new GoToTarget(),
                            new LookAtTarget(),
                            new CanDropOffAtTarget(),
                            new DropOffCargo(),
                            new TargetPrevious()
                        ),
                        //  Else attempt to collect resources
                        new BehaviorSequence(
                            new BehaviorSelector(
                                //  Try the current target
                                new CanCollectTarget(),
                                //  Or find the nearest matching resource
                                new TargetNearestCargoResource()
                            ),
                            //  Navigate to the resource
                            new GoToTarget(),
                            //  Collect the resource
                            new BehaviorSequence(
                                //  If cargo isn't full
                                new BehaviorInverter(
                                    new IsCargoFull()
                                ),
                                new CanCollectTarget(),
                                new SetCargoTypeFromTarget(),
                                new SetStateToGathering(),
                                new BehaviorDelay(1.5f,
                                    new CollectCargo()
                                )
                            )
                        ),
                        //  Else order is complete
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                ),

                new OrderIs(UnitOrder.DropOff,
                    new BehaviorSelector(
                        //  Attempt to drop off at the target
                        new BehaviorSequence(
                            new HasTarget(),
                            new GoToTarget(),
                            new CanDropOffAtTarget(),
                            new DropOffCargo(),
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

                new OrderIs(UnitOrder.Hunt,
                    new BehaviorSelector(
                        //  Attempt to drop off if cargo is full
                        new BehaviorSequence(
                            new IsCargoFull(),
                            new BehaviorSelector(
                                //  Try the current target
                                new CanDropOffAtTarget(),
                                //  Or get the nearest dropoff
                                new TargetNearestDropOff()
                            ),
                            new GoToTarget(),
                            new LookAtTarget(),
                            new CanDropOffAtTarget(),
                            new DropOffCargo(),
                            new TargetPrevious()
                        ),
                        //  Else attempt to hunt a target
                        new BehaviorSequence(
                            new BehaviorSelector(
                                //  Try the current target
                                new HasTarget(),
                                //  Or find the nearest matching resource
                                new TargetNearestFauna()
                            ),
                            //  Navigate to the target
                            new GoToTarget(),
                            new BehaviorSelector(
                                //  Try to collect the target
                                new BehaviorSequence(
                                    //  If cargo isn't full
                                    new BehaviorInverter(
                                        new IsCargoFull()
                                    ),
                                    new CanCollectTarget(),
                                    new SetCargoTypeFromTarget(),
                                    new SetStateToGathering(),
                                    new BehaviorDelay(1.5f,
                                        new CollectCargo()
                                    )
                                ),
                                //  Else try to attack the target
                                new BehaviorSelector(
                                    new BehaviorSequence(
                                        new SetActorState(ActorAnimationState.HUNTING),
                                        new BehaviorDelay(1.5f,
                                            new AttackTarget()
                                        ),
                                        new SetCargoType(ResourceGatheringType.Meat),
                                        new TargetNearestCargoResource()
                                    )
                                )
                            )
                        ),
                        //  Else order is complete
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                ),

                new OrderIs(UnitOrder.Attack,
                    new BehaviorSelector(
                        //  Attempt to chase and attack the target
                        new BehaviorSequence(
                            new HasTarget(),
                            new GoToTarget(),
                            new SetActorState(ActorAnimationState.HUNTING),
                            new BehaviorDelay(1.5f,
                                new AttackTarget()
                            ),
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

                new OrderIs(UnitOrder.Repair,
                    new BehaviorSelector(
                        //  Attempt to repair the target
                        new BehaviorSequence(
                            new HasTarget(),
                            new GoToTarget(),
                            new SetActorState(ActorAnimationState.BUILDANDREPAIR),
                            new BehaviorDelay(1.5f,
                                new HealTarget()
                            ),
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