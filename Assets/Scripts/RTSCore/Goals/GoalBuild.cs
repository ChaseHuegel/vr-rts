using UnityEngine;
using Swordfish.Navigation;

public class GoalBuild: PathfindingGoal
{
    //public ResourceGatheringType type;

    public override bool CheckGoal(Cell cell)
    {
        Structure structure = cell.GetOccupant<Structure>();

        if (structure != null && structure.GetComponent<TerrainBuilding>().NeedsBuilding())
            return true;

        return false;
    }
}