using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;

public class Structure : Obstacle, IFactioned
{
    public byte factionID = 0;
    private Faction faction;

    public ResourceGatheringType dropoffTypes;

    public Faction GetFaction() { return faction; }
    public void UpdateFaction() { faction = GameMaster.Factions.Find(x => x.index == factionID); }

    public override void Initialize()
    {
        base.Initialize();

        UpdateFaction();
    }
}
