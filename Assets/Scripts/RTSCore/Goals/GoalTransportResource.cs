using UnityEngine;
using Swordfish.Navigation;

public class GoalTransportResource : PathfindingGoal
{
    public ResourceGatheringType type;

    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Structure structure = cell?.GetFirstOccupant<Structure>();

        if (structure != null && structure.CanDropOff(type) && structure.IsSameFaction(actor))
            return true;

        return false;
    }
}