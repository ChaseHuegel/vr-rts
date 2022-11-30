using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class TargetNearestInjuredFriendly : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 actor, float delta)
    {
        Body nearestFriendly = null;
        int shortestDistance = int.MaxValue;
        for (int i = 0; i < Body.AllBodies.Count; i++)
        {
            Body body = Body.AllBodies[i];

            if (!body.IsAlive() || body.Faction == null ||
                !body.Faction.IsAllied(actor.Faction) ||
                body == actor ||
                body.Attributes.Get(AttributeType.HEALTH).IsMax())
                continue;

            int distance = actor.GetDistanceTo(body.GetPosition().x, body.GetPosition().y);
            if (distance < shortestDistance && distance < actor.Attributes.ValueOf(AttributeType.SENSE_RADIUS))
            {
                shortestDistance = distance;
                nearestFriendly = body;
            }
        }

        if (nearestFriendly != null)
        {
            actor.Target = nearestFriendly;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}