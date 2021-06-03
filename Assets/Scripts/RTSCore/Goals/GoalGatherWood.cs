using UnityEngine;
using Swordfish.Navigation;

public class GoalGatherWood : PathfindingGoal
{
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Resource resource = cell?.GetFirstOccupant<Resource>();

        if (resource != null && resource.type.HasFlag(ResourceGatheringType.Wood) && 
            resource.AddInteractor(actor))
            return true;

        return false;
    }
}