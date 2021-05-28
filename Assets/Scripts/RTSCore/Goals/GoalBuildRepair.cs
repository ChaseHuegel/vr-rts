using UnityEngine;
using Swordfish.Navigation;
using UnityEditor;

public class GoalBuildRepair: PathfindingGoal
{
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Structure structure = cell?.GetFirstOccupant<Structure>();
        Constructible construction = cell?.GetFirstOccupant<Constructible>();

        if (structure != null && structure.NeedsRepairs())
            return true;

        if (construction != null && !construction.IsBuilt())
            return true;

        return false;
    }
}