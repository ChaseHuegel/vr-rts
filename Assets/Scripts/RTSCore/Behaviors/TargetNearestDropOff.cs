using Swordfish.Library.BehaviorTrees;

public class TargetNearestDropOff : BehaviorNode<VillagerV2>
{
    public override BehaviorState Evaluate(VillagerV2 target, float delta)
    {
        Structure nearestDropOff = null;
        int shortestDistance = int.MaxValue;
        for (int i = 0; i < Structure.AllStructures.Count; i++)
        {
            Structure structure = Structure.AllStructures[i];

            if (!structure.CanDropOff(target.CargoType))
                continue;

            int distance = target.GetDistanceTo(structure.GetPosition().x, structure.GetPosition().y);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestDropOff = structure;
            }
        }

        if (nearestDropOff != null)
        {
            target.Target = nearestDropOff;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
