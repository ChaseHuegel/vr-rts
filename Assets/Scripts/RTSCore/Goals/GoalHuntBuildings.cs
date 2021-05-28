using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

public class GoalHuntBuildings: PathfindingGoal
{
    public int myFactionID;   

    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Structure structure = cell.GetFirstOccupant<Structure>();
        if (structure && structure.factionID != myFactionID)
            return true;

        Constructible construction = cell.GetFirstOccupant<Constructible>();
        if (construction && construction.factionID != myFactionID)
            return true;
            
        return false;
    }
}
