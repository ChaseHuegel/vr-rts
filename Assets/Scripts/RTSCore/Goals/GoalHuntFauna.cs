using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

public class GoalHuntFauna: PathfindingGoal
{   
    public override bool CheckGoal(Cell cell)
    {
        Fauna fauna = cell?.GetFirstOccupant<Fauna>();

        if (fauna)
            return true;
            
        return false;
    }
}
