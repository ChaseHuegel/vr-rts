using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class ResetOrder : BehaviorAction<ActorV2>
{
    public override void Run(ActorV2 target, float delta)
    {
        target.Order = UnitOrder.None;
    }
}
