using UnityEngine;
using Swordfish.Navigation;

public class GoalTransportResource : PathfindingGoal
{
    public ResourceGatheringType type;
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Structure structure = cell?.GetFirstOccupant<Structure>();

        if (structure == null)
            return false;

        if (!structure.IsSameFaction(actor)) 
            return false;

        if (!structure.CanDropOff(type)) 
            return false;

        return true;
    }
}