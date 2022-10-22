using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class HealTarget : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 actor, float delta)
    {
        if (actor.Target.Attributes.Get(AttributeConstants.HEALTH).IsMax())
            return BehaviorState.SUCCESS;

        actor.Target.Heal(5, AttributeChangeCause.HEALED, actor);
        return BehaviorState.RUNNING;
    }
}
