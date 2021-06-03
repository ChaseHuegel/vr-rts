using Swordfish;
using UnityEngine;

[CreateAssetMenu(fileName = "New Faction", menuName = "RTS/Faction")]
public class Faction : ScriptableObject
{
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
        return Bit.Compare(mask, faction.mask, faction.Id);
    }
    
    // public bool IsSameFaction(Actor actor)
    // {
    //     return this.factionId == actor.factionId;
    // }

    public bool IsSameFaction(byte factionId)
    {
        return this.Id == factionId;
    }
}

public interface IFactioned
{
    Faction GetFaction();
}