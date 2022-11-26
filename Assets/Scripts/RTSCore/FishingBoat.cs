using Swordfish.Navigation;
using UnityEngine;

public class FishingBoat : VillagerV2
{
    protected override void OnLoadUnitData(UnitData data)
    {
        base.OnLoadUnitData(data);
        Attributes.Get(AttributeType.COLLECT_RATE).Value = data.fishingRate;
    }

    public override void IssueTargetedOrder(Body body)
    {
        switch (body)
        {
            case Resource resource:
                Target = resource;
                Order = UnitOrder.Collect;
                break;

            case Structure structure:
                Target = structure;
                if (structure.Attributes.Get(AttributeType.HEALTH).IsMax())
                    Order = UnitOrder.DropOff;
                break;

            default:
                Target = body;
                Order = UnitOrder.None;
                break;
        }
    }

    protected override void UpdateCurrentToolObject()
    {
        //  Do nothing
    }

    protected override void UpdateCurrentCargoObject(bool visible)
    {
        //  Do nothing
    }
}
