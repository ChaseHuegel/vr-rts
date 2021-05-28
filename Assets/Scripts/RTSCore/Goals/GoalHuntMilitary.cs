using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

[System.Serializable]
public class GoalHuntMilitary: PathfindingGoal
{
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Unit unit = cell.GetFirstOccupant<Unit>();

        if (unit && !unit.IsCivilian() && !unit.isDying && !unit.IsSameFaction(actor))
            return true;

        return false;
    }
}
