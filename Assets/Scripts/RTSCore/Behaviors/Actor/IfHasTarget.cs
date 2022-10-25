using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class IfHasTarget : BehaviorGate<ActorV2>
{
    public IfHasTarget(BehaviorNode child) : base(child)
    {
    }

    public override bool Check(ActorV2 actor, float delta)
    {
        return actor.Target != null;
    }
}
