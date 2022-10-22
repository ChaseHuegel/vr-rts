using Swordfish.Library.BehaviorTrees;
using UnityEngine;

public class DropOffCargo : BehaviorAction<VillagerV2>
{
    public override void Run(VillagerV2 villager, float delta)
    {
        GameMaster.SendFloatingIndicator(villager.Target.transform.position, $"+{villager.Cargo} {villager.CargoType}", Color.green);

        PlayerManager.instance.AddResourceToStockpile(villager.CargoType, villager.Cargo);
        villager.Cargo = 0;
    }
}
