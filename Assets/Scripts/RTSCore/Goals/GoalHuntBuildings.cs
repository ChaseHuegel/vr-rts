using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

public class GoalHuntBuildings: PathfindingGoal
{
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Structure structure = cell.GetFirstOccupant<Structure>();
        if (structure && structure.IsSameFaction(actor))
            return true;

        Constructible construction = cell.GetFirstOccupant<Constructible>();
        if (construction && construction.IsSameFaction(actor))
            return true;
            
        return false;
    }
}
