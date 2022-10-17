using Swordfish.Library.BehaviorTrees;

public class AddCargo : BehaviorAction<VillagerV2>
{
    public override void Run(VillagerV2 target, float delta)
    {
        target.Cargo += 1;
    }
}
