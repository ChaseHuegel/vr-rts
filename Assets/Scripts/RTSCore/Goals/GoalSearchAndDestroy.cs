using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

public class GoalSearchAndDestroy: PathfindingGoal
{
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Unit unit = cell.GetFirstOccupant<Unit>();
        if (unit && !unit.isDying && unit.IsSameFaction(actor))
            return true;

        Structure structure = cell.GetFirstOccupant<Structure>();
        if (structure && structure.IsSameFaction(actor))
            return true;

        Constructible construction = cell.GetFirstOccupant<Constructible>();
        if (construction && construction.IsSameFaction(actor))
            return true;
            
        return false;
    }
}
