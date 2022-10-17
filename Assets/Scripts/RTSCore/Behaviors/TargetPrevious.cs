using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class TargetPrevious : BehaviorAction<ActorV2>
{
    public override void Run(ActorV2 target, float delta)
    {
        target.Target = target.LastTarget;
    }
}
