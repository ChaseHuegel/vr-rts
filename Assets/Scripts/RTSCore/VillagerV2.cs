using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class VillagerV2 : ActorV2
{
    //  ! This is a test class don't try to actually use it.
    protected override BehaviorTree<ActorV2> BehaviorTree { get; set; }

    public int Cargo = 0;

    public override void Initialize()
    {
        base.Initialize();

        BehaviorTree = new BehaviorTree<ActorV2>(
            new BehaviorSelector(
                new OrderIsGoTo(
                    new HasDestination(
                        new BehaviorSequence(
                            new GoToDestination(),
                            new ResetDestination(),
                            new ResetOrder()
                        )
                    )
                ),
                new OrderIsCollect(
                    new HasTarget(
                        new BehaviorSequence(
                            new GoToTarget(),
                            new BehaviorDelay(1f,
                                new AddCargo()
                            )
                        )
                    )
                )
            )
        );
    }
}
