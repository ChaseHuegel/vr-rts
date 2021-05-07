using UnityEngine;
using Swordfish.Navigation;

public class GoalStoneMining : PathfindingGoal
{
    public override bool CheckGoal(Cell cell)
    {
        Resource resource = cell?.GetOccupant<Resource>();

        if (resource != null && resource.type == ResourceGatheringType.Stone)
            return true;

        return false;
    }

    public override void OnFoundGoal(Actor actor, Cell cell)
    {
        Villager villager = (Villager)actor;

        villager.state = UnitState.GATHERING;
    }

    public override void OnReachedGoal(Actor actor, Cell cell)
    {
        Resource resource = cell?.GetOccupant<Resource>();
        Villager villager = (Villager)actor;

        villager.TryGather(resource);
    }
}