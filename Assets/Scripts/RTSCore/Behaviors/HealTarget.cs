using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class HealTarget : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 target, float delta)
    {
        if (!target.Target.TryGetComponent(out Damageable victim))
            return BehaviorState.FAILED;

        if (victim.GetAttribute(Attributes.HEALTH).IsMax())
            return BehaviorState.SUCCESS;

        victim.Heal(5, AttributeChangeCause.HEALED);
        return BehaviorState.RUNNING;
    }
}
