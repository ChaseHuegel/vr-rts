using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class AttackTarget : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 target, float delta)
    {
        if (!target.Target.TryGetComponent(out Damageable victim))
            return BehaviorState.FAILED;

        if (victim.Attributes.ValueOf(AttributeConstants.HEALTH) == 0)
            return BehaviorState.SUCCESS;

        victim.Damage(5, AttributeChangeCause.ATTACKED, target.Damageable, DamageType.NONE);
        return BehaviorState.RUNNING;
    }
}
