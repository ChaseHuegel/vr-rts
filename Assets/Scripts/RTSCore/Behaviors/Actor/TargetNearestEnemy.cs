using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class TargetNearestEnemy : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 actor, float delta)
    {
        Body nearestEnemy = null;
        int shortestDistance = int.MaxValue;
        for (int i = 0; i < Body.AllBodies.Count; i++)
        {
            Body body = Body.AllBodies[i];

            if (!body.IsAlive() || body.Faction == null || body.Faction.IsAllied(actor.Faction))
                continue;

            int distance = actor.GetDistanceTo(body.GetPosition().x, body.GetPosition().y);
            if (distance < shortestDistance && distance < actor.Attributes.ValueOf(AttributeConstants.SENSE_RADIUS))
            {
                shortestDistance = distance;
                nearestEnemy = body;
            }
        }

        if (nearestEnemy != null)
        {
            actor.Target = nearestEnemy;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
