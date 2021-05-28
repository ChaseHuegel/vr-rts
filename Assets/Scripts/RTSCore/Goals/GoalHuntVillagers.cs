using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

[System.Serializable]
public class GoalHuntVillagers: PathfindingGoal
{   
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Villager villager = cell?.GetFirstOccupant<Villager>();

        if (villager  && !villager.isDying && villager.IsSameFaction(actor))
            return true;
            
        return false;
    }
}
