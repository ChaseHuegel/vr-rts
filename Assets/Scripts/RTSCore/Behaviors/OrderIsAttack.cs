using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class OrderIsAttack : BehaviorGate<ActorV2>
{
    public OrderIsAttack(BehaviorNode child) : base(child)
    {
    }

    public override bool Check(ActorV2 target, float delta)
    {
        return target.Order == UnitOrder.Attack;
    }
}
