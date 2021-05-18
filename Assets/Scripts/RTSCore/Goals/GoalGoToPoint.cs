using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;

public class GoalGoToPoint: PathfindingGoal
{
    public override bool CheckGoal(Cell cell)
    {
        return false;
    }
}
