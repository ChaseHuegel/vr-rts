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
                        new IfHasDestination(
                            new BehaviorSequence(
                                new GoToDestination(),
                                new ResetDestination(),
                                new ResetOrder()
                            )
                        ),
                        new BehaviorSequence(
                            new ResetDestination(),
                            new ResetOrder()
                        )
                    )
                ),

                new OrderIsCollect(
                    new BehaviorSelector(
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
                        new IfHasTarget(
                            new BehaviorSequence(
                                new BehaviorInverter(
                                    new IsCargoFull()
                                ),
                                new GoToTarget(),
                                new CanCollectTarget(),
                                new BehaviorDelay(1f,
                                    new CollectCargo()
                                )
                            )
                        ),
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                ),

                new OrderIsDropOff(
                    new BehaviorSelector(
                        new IfHasTarget(
                            new BehaviorSequence(
                                new GoToTarget(),
                                new CanDropOffAtTarget(),
                                new DropOffCargo(),
                                new ResetTarget(),
                                new ResetOrder()
                            )
                        ),
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                )
            )
        );
    }
}
