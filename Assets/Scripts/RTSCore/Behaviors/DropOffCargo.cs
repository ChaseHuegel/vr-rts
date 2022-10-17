using Swordfish.Library.BehaviorTrees;
using UnityEngine;

public class DropOffCargo : BehaviorAction<VillagerV2>
{
    public override void Run(VillagerV2 target, float delta)
    {
        GameMaster.SendFloatingIndicator(target.Target.transform.position, $"+{target.Cargo} {target.CargoType}", Color.green);

        PlayerManager.instance.AddResourceToStockpile(target.CargoType, target.Cargo);
        target.Cargo = 0;
    }
}
