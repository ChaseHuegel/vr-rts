using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class TargetNearestFauna : BehaviorNode<ActorV2>
{
    public override BehaviorState Evaluate(ActorV2 target, float delta)
    {
        Fauna nearestFauna = null;
        int shortestDistance = int.MaxValue;
        for (int i = 0; i < Fauna.AllFauna.Count; i++)
        {
            Fauna fauna = Fauna.AllFauna[i];

            int distance = target.DistanceTo(fauna);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestFauna = fauna;
            }
        }

        if (nearestFauna != null)
        {
            target.Target = nearestFauna;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
