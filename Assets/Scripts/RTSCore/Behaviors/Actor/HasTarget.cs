using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class HasTarget : BehaviorCondition<ActorV2>
{
    public override bool Check(ActorV2 actor, float delta)
    {
        return actor.Target != null;
    }
}
