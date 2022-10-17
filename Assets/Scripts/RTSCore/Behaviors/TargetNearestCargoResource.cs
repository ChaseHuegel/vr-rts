using Swordfish.Library.BehaviorTrees;

public class TargetNearestCargoResource : BehaviorNode<VillagerV2>
{
    public override BehaviorState Evaluate(VillagerV2 target, float delta)
    {
        Resource nearestResource = null;
        int shortestDistance = int.MaxValue;
        for (int i = 0; i < Resource.AllResources.Count; i++)
        {
            Resource resource = Resource.AllResources[i];

            if (resource.type != target.CargoType)
                continue;

            int distance = target.DistanceTo(resource);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestResource = resource;
            }
        }

        if (nearestResource != null)
        {
            target.Target = nearestResource;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
