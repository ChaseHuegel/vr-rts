using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class Structure : Obstacle, IFactioned
{
    public byte factionID = 0;
    private Faction faction;

    public ResourceGatheringType dropoffTypes;

    public bool built = false;
    private Damageable damageable;
    public Damageable AttributeHandler { get { return damageable; } }

    public Faction GetFaction() { return faction; }
    public void UpdateFaction() { faction = GameMaster.Factions.Find(x => x.index == factionID); }

    public bool NeedsRepairs() { return damageable.GetAttributePercent(Attributes.HEALTH) < 1f; }
    public bool IsBuilt() { return built; }

    public void TryRepair(int count, Actor repairer = null)
    {
        AttributeHandler.Heal(count, AttributeChangeCause.HEALED, repairer.AttributeHandler);

        if (damageable.GetAttributePercent(Attributes.HEALTH) >= 1f)
            built = true;
    }

    public override void Initialize()
    {
        base.Initialize();

        UpdateFaction();

        damageable = GetComponent<Damageable>();
        if (!damageable)
            Debug.Log("No damageable component on structure!");
    }
}
