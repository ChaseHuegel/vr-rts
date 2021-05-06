using UnityEngine;
using Swordfish.Navigation;

public class GoalTransportWood : PathfindingGoal
{
    public override bool CheckGoal(Cell cell)
    {
        Structure structure = cell.GetOccupant<Structure>();

        if (structure != null && structure.dropoffTypes.Contains(ResourceGatheringType.Wood))
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

        //  TODO add cargo to player's resources

        //  Dump our cargo
        villager.currentCargo = 0;
    }
}