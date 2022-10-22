using Swordfish.Library.BehaviorTrees;
using UnityEngine;

public class DropOffCargo : BehaviorAction<VillagerV2>
{
    public override void Run(VillagerV2 villager, float delta)
    {
        var cargo = villager.Attributes.Get(AttributeConstants.CARGO);

        GameMaster.SendFloatingIndicator(villager.Target.transform.position, $"+{(int)cargo.Value} {villager.CargoType}", Color.green);

        PlayerManager.instance.AddResourceToStockpile(villager.CargoType, (int)cargo.Value);
        cargo.Value = 0;
    }
}
