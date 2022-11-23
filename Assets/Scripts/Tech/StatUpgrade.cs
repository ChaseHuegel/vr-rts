using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Swordfish;
using Swordfish.Library.Collections;

public enum AttributeType
{
    NONE = 0,
    hp = 1,
    spd = 2,
    reach = 4,
    cargo = 8,
    dmg = 16,
    armr = 32,
    sensor = 64,
    atkspd = 128,
    atkrng = 256,
    collectRate = 512, // Overall collect rate multiplier applied to all gathering
    healRate = 1024,
    bldRate = 2048,
    rprRate = 4096,
    stnMnRate = 8192,
    gldMnRate = 16384,
    frmRate = 32768,
    hntgRate = 65536,
    lmbjkRate = 131072,
    fshRate = 262144,
    BLUDGEONING_DAMAGE = 524288,
    PIERCING_DAMAGE = 1048576,
    SLASHING_DAMAGE = 2097152,
    HACKING_DAMAGE = 4194304,
}

[Serializable]
public class AttributeBonus
{
    public AttributeType targetAttribute;       
    public float amount;

    public AttributeBonus(AttributeType attributeType, float fAmount)    
    {
        targetAttribute = attributeType;
        amount = fAmount;
    }
}

[CreateAssetMenu(fileName = "Stat Upgrade", menuName = "RTS/Tech/Stat Upgrade")]
public class StatUpgrade : TechBase
{
    public List<UnitData> targetUnits;
    public List<AttributeBonus> attributeBonuses;
    
    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        foreach (UnitData unitData in targetUnits)
            foreach (AttributeBonus attributeBonus in attributeBonuses)
                PlayerManager.Instance.AddUnitStatUpgrade(unitData, attributeBonus);

        Debug.Log(this.title + "research completed.");
    }    
}
