using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class VillagerV2 : ActorV2
{
    //  ! This is a test class don't try to actually use it.
    protected override BehaviorTree<ActorV2> BehaviorTree { get; set; }

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
                )
            )
        );
    }
}
