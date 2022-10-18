using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class LookAtTarget : BehaviorAction<ActorV2>
{
    public override void Run(ActorV2 target, float delta)
    {
        target.LookAt(target.Target.gridPosition.x, target.Target.gridPosition.y);
    }
}