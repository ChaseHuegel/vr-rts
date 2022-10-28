using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class TargetIsEnemy : BehaviorCondition<ActorV2>
{
    public override bool Check(ActorV2 actor, float delta)
    {
        return !actor.Faction?.IsAllied(actor.Target?.Faction) ?? true;
    }
}
