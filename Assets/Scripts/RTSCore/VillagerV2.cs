using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;
using UnityEngine;

public class VillagerV2 : ActorV2
{
    //  ! This is a test class don't try to actually use it.
    protected override BehaviorTree<ActorV2> BehaviorTree { get; set; }

    public bool IsCargoFull => Cargo >= 10;

    public int Cargo = 0;
    public ResourceGatheringType CargoType = ResourceGatheringType.None;

    public Animator Animator;

    public override void Initialize()
    {
        base.Initialize();

        Animator = gameObject.GetComponentInChildren<Animator>();

        BehaviorTree = new BehaviorTree<ActorV2>(
            new BehaviorSelector(

                new OrderIsGoTo(
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

                new OrderIsCollect(
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
                            new BehaviorDelay(1f,
                                new BehaviorSequence(
                                    //  If cargo isn't full
                                    new BehaviorInverter(
                                        new IsCargoFull()
                                    ),
                                    new CanCollectTarget(),
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

                new OrderIsDropOff(
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

                //  Try to drop off cargo if full
                new BehaviorSequence(
                    new IsCargoFull(),
                    new BehaviorSelector(
                        new CanDropOffAtTarget(),
                        new TargetNearestDropOff()
                    ),
                    new GoToTarget(),
                    new CanDropOffAtTarget(),
                    new DropOffCargo(),
                    new TargetPrevious()
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

                //  Wander out of boredom
                new BehaviorSequence(
                    new BehaviorDelay(2f,
                        new RandomizeDestination(5)
                    )
                )
            )
        );
    }
}
