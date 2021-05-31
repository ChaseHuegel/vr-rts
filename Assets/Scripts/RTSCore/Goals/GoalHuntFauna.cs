using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

public class GoalHuntFauna: PathfindingGoal
{   
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Fauna fauna = cell?.GetFirstOccupant<Fauna>();

        if (fauna && !fauna.IsDead())
            return true;
            
        return false;
    }
}
