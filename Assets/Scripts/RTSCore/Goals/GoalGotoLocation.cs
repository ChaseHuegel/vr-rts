using UnityEngine;
using Swordfish.Navigation;

public class GoalGotoLocation : PathfindingGoal
{
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        if (!cell.occupied)
            return true;

        return false;
    }
}