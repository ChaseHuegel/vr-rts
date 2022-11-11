using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class TargetIsConstructibleWall : BehaviorCondition<ActorV2>
{
    public override bool Check(ActorV2 actor, float delta)
    {
        Constructible constructible = actor.Target.GetComponent<Constructible>();
        if (constructible)
        {
            if (constructible.GetComponent<WallSegment>())
                return true;
        }

        return false;
    }
}
