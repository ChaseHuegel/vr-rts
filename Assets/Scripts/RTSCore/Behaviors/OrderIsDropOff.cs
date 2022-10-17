using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class OrderIsDropOff : BehaviorGate<ActorV2>
{
    public OrderIsDropOff(BehaviorNode child) : base(child)
    {
    }

    public override bool Check(ActorV2 target, float delta)
    {
        return target.Order == UnitOrder.DropOff;
    }
}
