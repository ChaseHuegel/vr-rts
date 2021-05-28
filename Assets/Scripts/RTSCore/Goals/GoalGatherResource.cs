using UnityEngine;
using Swordfish.Navigation;

public class GoalGatherResource : PathfindingGoal
{
    public ResourceGatheringType type;

    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Resource resource = cell?.GetFirstOccupant<Resource>();

        if (resource != null && resource.type.HasFlag(type) && 
            resource.AddInteractor(actor))
            return true;

        return false;
    }
}