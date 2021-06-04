using UnityEngine;
using Swordfish.Navigation;

public class GoalGotoLocation : PathfindingGoal
{
    public int x = 0;
    public int y = 0;

    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        if (!cell.occupied && cell.x == x && cell.y == y)
            return true;

        return false;
    }
}