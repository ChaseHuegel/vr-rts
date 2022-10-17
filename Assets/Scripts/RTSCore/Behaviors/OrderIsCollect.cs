using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class OrderIsCollect : BehaviorGate<ActorV2>
{
    public OrderIsCollect(BehaviorNode child) : base(child)
    {
    }

    public override bool Check(ActorV2 target, float delta)
    {
        return target.Order == UnitOrder.Collect;
    }
}
