using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

[System.Serializable]
public class GoalHuntMilitary: PathfindingGoal
{
    public int myFactionID;
    public override bool CheckGoal(Cell cell)
    {
        Unit unit = cell.GetFirstOccupant<Unit>();

        if (unit && !unit.IsCivilian() && !unit.isDying && unit.factionID != myFactionID)
                return true;

        return false;
    }
}
