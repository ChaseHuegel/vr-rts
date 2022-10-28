using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class SoldierV2 : UnitV2
{
    public override bool IsCivilian => false;

    protected override BehaviorTree<ActorV2> BehaviorTreeFactory()
    {
        return SoldierBehaviorTree.Value;
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeConstants.ARMOR, 1f);
    }

    public override void IssueTargetedOrder(Body body)
    {
        switch (body)
        {
            case ActorV2 _:
                Target = body;
                if (body.Faction == null || !body.Faction.IsAllied(Faction))
                    Order = UnitOrder.Attack;
                else
                    Order = UnitOrder.Follow;
                break;

            case Structure _:
            case Constructible _:
                Target = body;
                if (body.Faction == null || !body.Faction.IsAllied(Faction))
                    Order = UnitOrder.Attack;
                else
                    Order = UnitOrder.GoTo;
                break;

            default:
                Target = body;
                Order = UnitOrder.GoTo;
                break;
        }
    }

    protected override void OnDamaged(DamageEvent e)
    {
        base.OnDamaged(e);
        if (Target == null && e.attacker is Body attacker)
            IssueTargetedOrder(attacker);
    }
}
