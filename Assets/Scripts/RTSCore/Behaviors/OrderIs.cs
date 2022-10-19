using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class OrderIs : BehaviorGate<ActorV2>
{
    private readonly UnitOrder Order;

    public OrderIs(UnitOrder order, BehaviorNode child) : base(child)
    {
        Order = order;
    }

    public override bool Check(ActorV2 target, float delta)
    {
        return target.Order == Order;
    }
}
