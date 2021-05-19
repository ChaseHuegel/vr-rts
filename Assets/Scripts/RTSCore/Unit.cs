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

    public virtual void SetUnitData(UnitData unitData)
    {
        m_rtsUnitTypeData = unitData;
    }

    public virtual void SetUnitType(RTSUnitType unitType)
    {
        rtsUnitType = unitType;
    }

    public virtual bool IsCivilian()
    {
        return (int)rtsUnitTypeData.unitType < (int)RTSUnitType.Scout;
    }
}
