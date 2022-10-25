using Swordfish.Library.BehaviorTrees;

public class TargetNearestCargoResource : BehaviorNode<VillagerV2>
{
    public override BehaviorState Evaluate(VillagerV2 villager, float delta)
    {
        Resource nearestResource = null;
        int shortestDistance = int.MaxValue;
        for (int i = 0; i < Resource.AllResources.Count; i++)
        {
            Resource resource = Resource.AllResources[i];

            if (resource.type != villager.CargoType)
                continue;

            int distance = villager.GetDistanceTo(resource.GetPosition().x, resource.GetPosition().y);
            if (distance < shortestDistance && distance < villager.Attributes.ValueOf(AttributeConstants.SENSE_RADIUS))
            {
                shortestDistance = distance;
                nearestResource = resource;
            }
        }

        if (nearestResource != null)
        {
            villager.Target = nearestResource;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
