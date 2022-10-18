using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class OrderIsHunt : BehaviorGate<ActorV2>
{
    public OrderIsHunt(BehaviorNode child) : base(child)
    {
    }

    public override bool Check(ActorV2 target, float delta)
    {
        return target.Order == UnitOrder.Hunt;
    }
}
