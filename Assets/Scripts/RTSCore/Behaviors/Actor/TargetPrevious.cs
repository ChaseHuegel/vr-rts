using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class TargetPrevious : BehaviorAction<ActorV2>
{
    public override void Run(ActorV2 actor, float delta)
    {
        actor.Target = actor.LastTarget;
    }
}
