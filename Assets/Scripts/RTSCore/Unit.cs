using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public enum UnitState
{
    IDLE,
    ROAMING,
    GATHERING,
    TRANSPORTING,
    BUILDANDREPAIR,
    RALLYING,
}

public class Unit : Actor, IFactioned
{
    [Header("Faction")]
    public byte factionID = 0;
    private Faction faction;

    public Faction GetFaction() { return faction; }
    public void UpdateFaction() { faction = GameMaster.Factions.Find(x => x.index == factionID); }

    [Header("Unit")]
    public RTSUnitType rtsUnitType;

    [Header("AI")]    
    public UnitState state;
    
    protected UnitState previousState;

    // Make this read only, we should only be able to change unit properties
    // through the database.
    public UnitData rtsUnitTypeData { get { return m_rtsUnitTypeData; } }
    protected UnitData m_rtsUnitTypeData;

    public override void Initialize()
    {
        base.Initialize();

        if (!m_rtsUnitTypeData)
            m_rtsUnitTypeData = GameMaster.GetUnit(rtsUnitType);

        UpdateFaction();
    }

    //=========================================================================
    // Sets the unit type and unitTypeData
    public virtual void SetUnitType(RTSUnitType unitType)
    {
        rtsUnitType = unitType;
        m_rtsUnitTypeData = GameMaster.GetUnit(rtsUnitType);
    }

    public virtual bool IsCivilian()
    {
        return (int)rtsUnitTypeData.unitType < (int)RTSUnitType.Scout;
    }
}
