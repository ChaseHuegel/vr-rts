using UnityEngine;
using Swordfish.Navigation;

public class GoalTransportStone : PathfindingGoal
{
    public override bool CheckGoal(Cell cell)
    {
        Structure structure = cell.GetOccupant<Structure>();

        if (structure != null && structure.dropoffTypes.Contains(ResourceGatheringType.Stone))
            return true;

        return false;
    }

    public override void OnFoundGoal(Actor actor, Cell cell)
    {
        Villager villager = (Villager)actor;

        villager.state = UnitState.TRANSPORTING;
    }

    public override void OnReachedGoal(Actor actor, Cell cell)
    {
        Resource resource = cell.GetOccupant<Resource>();
        Villager villager = (Villager)actor;

        PlayerManager.instance.AddResourceToStockpile(ResourceGatheringType.Stone, villager.currentCargo);

        //  Dump our cargo
        villager.currentCargo = 0;
    }
}