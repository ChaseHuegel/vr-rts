using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class HasTarget : BehaviorGate<ActorV2>
{
    public HasTarget(BehaviorNode child) : base(child)
    {
    }

    public override bool Check(ActorV2 target, float delta)
    {
        return target.Target != null;
    }
}
