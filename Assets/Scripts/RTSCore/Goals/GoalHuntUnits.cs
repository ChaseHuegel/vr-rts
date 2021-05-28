using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

public class GoalHuntUnits: PathfindingGoal
{   
    public int myFactionID;
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Unit unit = cell?.GetFirstOccupant<Unit>();

        if (unit && !unit.isDying && unit.factionID != myFactionID)
            return true;
            
        return false;
    }
}
