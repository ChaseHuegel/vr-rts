using Swordfish.Library.BehaviorTrees;
using UnityEngine;

public class DropOffCargo : BehaviorAction<VillagerV2>
{
    public override void Run(VillagerV2 villager, float delta)
    {
        var cargo = villager.Attributes.Get(AttributeType.CARGO);

        Vector3 pos = villager.Target.transform.position;
        pos.y += 0.5f;

        GameMaster.SendFloatingIndicator(pos, $"+{(int)cargo.Value} {villager.CargoType}", Color.green);

        PlayerManager.Instance.AddResourceToStockpile(villager.CargoType, (int)cargo.Value);
        cargo.Value = 0;
    }
}
