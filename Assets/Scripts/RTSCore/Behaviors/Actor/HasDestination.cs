using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class HasDestination : BehaviorCondition<ActorV2>
{
    public override bool Check(ActorV2 actor, float delta)
    {
        return actor.Destination != null;
    }
}
