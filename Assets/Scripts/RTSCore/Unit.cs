using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public class Unit : Actor, IFactioned
{
    [Header("Faction")]
    public byte factionID = 0;
    private Faction faction;

    public Faction GetFaction() { return faction; }
    public void UpdateFaction() { faction = GameMaster.Factions.Find(x => x.index == factionID); }

    public RTSUnitTypeData rtsUnitTypeData;
    
    public override void Initialize()
    {
        base.Initialize();
        
        // This could be removed at a later date and replaced with specific fetches
        // of the information needed in inheritors if we want to sacrifice memory
        // for performance
        rtsUnitTypeData = GameMaster.Instance.FindUnitData(rtsUnitTypeData.unitType);

        UpdateFaction();
    }
}
