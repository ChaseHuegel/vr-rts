using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class LookAtTarget : BehaviorAction<ActorV2>
{
    public override void Run(ActorV2 actor, float delta)
    {
        actor.LookAt(actor.Target.GetPosition().x, actor.Target.GetPosition().y);
    }
}
