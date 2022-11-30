using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class Priest : UnitV2
{
    public override bool IsCivilian => false;    

    protected override BehaviorTree<ActorV2> BehaviorTreeFactory()
    {
        return PriestBehaviorTree.Value;
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeType.ARMOR, 1f);
        Attributes.AddOrUpdate(AttributeType.HEAL_RATE, 1f);
    }

    protected override void OnLoadUnitData(UnitData data)
    {
        base.OnLoadUnitData(data);
        Attributes.AddOrUpdate(AttributeType.ARMOR, unitData.armor);
        
    }

    public override void IssueTargetedOrder(Body body)
    {
        switch (body)
        {
            case ActorV2 _:
                Target = body;
                if (body.Faction.IsAllied(Faction))
                    Order = UnitOrder.Heal;
                else
                    Order = UnitOrder.GoTo;
                break;

            case Structure _:
            case Constructible _:
                Target = body;
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

        // Target = this;
        // Order = UnitOrder.Repair;
    }
}