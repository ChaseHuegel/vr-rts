using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

public class GoalHuntUnits: PathfindingGoal
{   
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Unit unit = cell?.GetFirstOccupant<Unit>();

        if (unit && !unit.isDying && !unit.IsSameFaction(actor))
            return true;
            
        return false;
    }
}
