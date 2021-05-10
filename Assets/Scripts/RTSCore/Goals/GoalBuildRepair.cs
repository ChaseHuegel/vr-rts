using UnityEngine;
using Swordfish.Navigation;

public class GoalBuildRepair: PathfindingGoal
{
    public override bool CheckGoal(Cell cell)
    {
        Structure structure = cell?.GetOccupant<Structure>();

        if (structure != null && structure.GetComponent<TerrainBuilding>().NeedsRepair())
            return true;

        return false;
    }
}