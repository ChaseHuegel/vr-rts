using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class IfHasDestination : BehaviorGate<ActorV2>
{
    public IfHasDestination(BehaviorNode child) : base(child)
    {
    }

    public override bool Check(ActorV2 target, float delta)
    {
        return target.Destination != null;
    }
}
