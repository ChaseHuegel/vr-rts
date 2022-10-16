using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class HasDestination : BehaviorGate<ActorV2>
{
    public HasDestination(BehaviorNode child) : base(child)
    {
    }

    public override bool Check(ActorV2 target, float delta)
    {
        return target.Destination != null;
    }
}
