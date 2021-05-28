using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

[System.Serializable]
public class GoalHuntVillagers: PathfindingGoal
{   
    public int myFactionID;
    public override bool CheckGoal(Cell cell, Actor actor = null)
    {
        Villager villager = cell?.GetFirstOccupant<Villager>();

        if (villager  && !villager.isDying && villager.factionID != myFactionID)
            return true;
            
        return false;
    }
}
