using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;

public class TargetNearestConstructibleWall : BehaviorNode<VillagerV2>
{
    public override BehaviorState Evaluate(VillagerV2 villager, float delta)
    {
        Body nearestWall = null;
        int shortestDistance = int.MaxValue;
        for (int i = 0; i < Constructible.AllBodies.Count; i++)
        {
            Body body = Constructible.AllBodies[i];
            if (!body)
                continue;

            Constructible constructible = body.GetComponent<Constructible>();
            if (!constructible)
                continue;

            // if (constructible.GetComponent<WallSegment>() != null)
            if (constructible.buildingData is WallData)
            {
                int distance = villager.GetDistanceTo(body.GetPosition().x, body.GetPosition().y);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestWall = body;
                }
            }
        }

        if (nearestWall != null)
        {
            villager.Target = nearestWall;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
