using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class OrderIsGoTo : BehaviorGate<ActorV2>
{
    public OrderIsGoTo(BehaviorNode child) : base(child)
    {
    }

    public override bool Check(ActorV2 target, float delta)
    {
        return target.Order == UnitOrder.GoTo;
    }
}
