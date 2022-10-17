using Swordfish.Library.BehaviorTrees;

public class HasTarget : BehaviorCondition<VillagerV2>
{
    public override bool Check(VillagerV2 target, float delta)
    {
        return target.Target != null;
    }
}
