using Swordfish;
using UnityEngine;

[CreateAssetMenu(fileName = "New Faction", menuName = "RTS/Faction")]
public class Faction : ScriptableObject
{
    //  TODO we need a neutral faction that is the default so we don't need to perform null checks everywhere.
    //  It's clunky, and a null ref would be a good way to differentiate an unaffiliated unit vs we didn't assign a faction to it.
    //  AKA null refs should be a good thing for Faction to identify bugs.

    public byte Id;
    public Skin skin;
    public TechTree techTree;

    // TODO: Color should be pulled from the skin settings?
    public Color color = Color.blue;
    private BitMask mask;

    public void SetAlly(Faction faction)
    {
        Bit.Set(ref mask.bits, faction.Id);
    }

    public void SetEnemy(Faction faction)
    {
        Bit.Clear(ref mask.bits, faction.Id);
    }
}

public static class FactionExtensions
{
    public static bool IsAllied(this Faction faction, Faction other)
    {
        //  TODO the mask isn't being used currently
        return IsSameFaction(faction, other);//Bit.Compare(mask, faction.mask, faction.Id);
    }

    public static bool IsSameFaction(this Faction faction, Faction other)
    {
        return faction?.Id == other?.Id;
    }
}