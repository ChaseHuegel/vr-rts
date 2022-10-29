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

    public bool IsAllied(Faction faction)
    {
        //  TODO the mask isn't being used currently
        return IsSameFaction(faction);//Bit.Compare(mask, faction.mask, faction.Id);
    }

    public bool IsSameFaction(byte factionId)
    {
        return Id == factionId;
    }

    public bool IsSameFaction(Faction faction)
    {
        return Id == faction?.Id;
    }
}