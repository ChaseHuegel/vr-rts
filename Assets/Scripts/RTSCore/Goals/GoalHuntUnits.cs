using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

public class GoalHuntUnits: PathfindingGoal
{   
    public int factionId;
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Unit unit = cell?.GetFirstOccupant<Unit>();

        if (unit && !unit.isDying && unit.IsSameFaction(factionId))
            return true;
            
        return false;
    }
}
