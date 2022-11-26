using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class TargetNearestFauna : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 actor, float delta)
    {
        Fauna nearestFauna = null;
        int shortestDistance = int.MaxValue;
        for (int i = 0; i < Fauna.AllFauna.Count; i++)
        {
            Fauna fauna = Fauna.AllFauna[i];

            int distance = actor.GetDistanceTo(fauna.GetPosition().x, fauna.GetPosition().y);
            if (distance < shortestDistance && distance < actor.Attributes.ValueOf(AttributeType.SENSE_RADIUS))
            {
                shortestDistance = distance;
                nearestFauna = fauna;
            }
        }

        if (nearestFauna != null)
        {
            actor.Target = nearestFauna;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
