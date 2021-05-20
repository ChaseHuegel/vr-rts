using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

public class GoalSearchAndDestroy: PathfindingGoal
{
    public int myFactionID;   

    public override bool CheckGoal(Cell cell)
    {
        Unit unit = cell.GetFirstOccupant<Unit>();
        if (unit && unit.factionID != myFactionID)
            return true;

        Structure structure = cell.GetFirstOccupant<Structure>();
        if (structure && structure.factionID != myFactionID)
            return true;

        Constructible construction = cell.GetFirstOccupant<Constructible>();
        if (construction && construction.factionID != myFactionID)
            return true;
            
        return false;
    }
}
