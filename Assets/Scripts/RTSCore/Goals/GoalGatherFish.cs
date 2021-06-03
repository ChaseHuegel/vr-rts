using UnityEngine;
using Swordfish.Navigation;

public class GoalGatherFish : PathfindingGoal
{
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Resource resource = cell?.GetFirstOccupant<Resource>();

        if (resource != null && resource.type.HasFlag(ResourceGatheringType.Fish) && 
            resource.AddInteractor(actor))
            return true;

        return false;
    }
}