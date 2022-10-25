using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class AttackTarget : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 actor, float delta)
    {
        if (actor.Target.Attributes.ValueOf(AttributeConstants.HEALTH) == 0)
            return BehaviorState.SUCCESS;

        actor.Target.Damage(5, AttributeChangeCause.ATTACKED, actor, DamageType.NONE);
        return BehaviorState.RUNNING;
    }
}
