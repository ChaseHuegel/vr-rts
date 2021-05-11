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
        UpdateFaction();
    }
}
