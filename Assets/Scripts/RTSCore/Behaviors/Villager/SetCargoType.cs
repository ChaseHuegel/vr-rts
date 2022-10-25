using Swordfish.Library.BehaviorTrees;

public class SetCargoType : BehaviorAction<VillagerV2>
{
    private readonly ResourceGatheringType CargoType;

    public SetCargoType(ResourceGatheringType cargoType)
    {
        CargoType = cargoType;
    }

    public override void Run(VillagerV2 villager, float delta)
    {
        villager.CargoType = CargoType;
    }
}
