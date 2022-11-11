using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Library.Types;
using Swordfish.Navigation;

public class FishingBoat : VillagerV2
{
    protected override void OnLoadUnitData(UnitData data)
    {
        base.OnLoadUnitData(data);
        Attributes.Get(AttributeConstants.COLLECT_RATE).Value = data.fishingRate;
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
                if (structure.Attributes.Get(AttributeConstants.HEALTH).IsMax())
                    Order = UnitOrder.DropOff;
                break;

            default:
                Target = body;
                Order = UnitOrder.None;
                break;
        }
    }

    public override void OrderToCollect(ResourceGatheringType resourceType, Body target = null)
    {
        Target = target;
        Order = UnitOrder.Collect;
        CargoType = resourceType;
    }

    protected override void OnCargoChanged(object sender, DataChangedEventArgs<float> e)
    {
    }

    protected override void OnStateUpdate()
    {
        base.OnStateUpdate();
    }
}
