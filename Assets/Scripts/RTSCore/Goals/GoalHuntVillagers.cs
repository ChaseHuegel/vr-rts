using UnityEngine;
using Swordfish.Navigation;
using Swordfish;

[System.Serializable]
public class GoalHuntVillagers: PathfindingGoal
{   
    public int myFactionID;
    public override bool CheckGoal(Cell cell)
    {
        Villager villager = cell?.GetFirstOccupant<Villager>();

        if (villager != null && villager.factionID != myFactionID)
            return true;
            
        return false;
    }
}
