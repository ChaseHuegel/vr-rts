using UnityEngine;
using Swordfish.Navigation;

public class GoalGatherResource : PathfindingGoal
{
    public ResourceGatheringType type;

    public override bool CheckGoal(Cell cell)
    {
        Resource resource = cell?.GetOccupant<Resource>();

        if (resource != null && resource.type.HasFlag(type))
            return true;

        return false;
    }
}