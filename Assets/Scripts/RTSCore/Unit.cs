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

    public RTSUnitType rtsUnitType;

    // Make this read only, we should only be able to change unit properties
    // through the database.
    public UnitData rtsUnitTypeData { get { return m_rtsUnitTypeData; } }
    public UnitData m_rtsUnitTypeData;

    public override void Initialize()
    {
        base.Initialize();

        // TODO: This could be removed at a later date and replaced with specific fetches
        // of the information needed in inheritors if we want to sacrifice memory
        // for performance
        m_rtsUnitTypeData = GameMaster.GetUnit(rtsUnitType);
        
        UpdateFaction();
    }

    // TODO: Not fond of this setup but it works and I don't want to dig into
    // this mess right now.
    public void SetUnitData(UnitData unitData)
    {
        m_rtsUnitTypeData = unitData;
    }

    public bool IsCivilian()
    {
        return (int)rtsUnitTypeData.unitType < (int)RTSUnitType.Scout;
    }
}
