using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Swordfish;
using Swordfish.Library.Collections;

[Flags]
public enum AttributeUpgradeType
{
    NONE = 0,
    HEALTH = 1,
    SPEED = 2,
    REACH = 4,
    CARGO = 8,
    DAMAGE = 16,
    ARMOR = 32,
    SENSE_RADIUS = 64,
    ATTACK_SPEED = 128,
    ATTACK_RANGE = 256,
    COLLECT_RATE = 512, // Overall collect rate multiplier applied to all gathering
    HEAL_RATE = 1024,
    BUILD_RATE = 2048,
    REPAIR_RATE = 4096,
    STONE_MINING_RATE = 8192,
    GOLD_MINING_RATE = 16384,
    FARMING_RATE = 32768,
    HUNTING_RATE = 65536,
    LUMBERJACKING_RATE = 131072,
    FISHING_RATE = 262144,
}

[Serializable]
public class AttributeUpgrade
{
    public List<UnitData> targetUnits;
    public AttributeUpgradeType targetAttribute;
    public DamageType damageType;
    public float amount;
}

[CreateAssetMenu(fileName = "Stat Upgrade", menuName = "RTS/Tech/Stat Upgrade")]
public class StatUpgrade : TechBase
{
    public static List<AttributeUpgrade> AllAttributeBonuses;
    public List<AttributeUpgrade> attributeUpgrades;

    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        foreach (AttributeUpgrade attributeUpgrade in attributeUpgrades)
        {
            AllAttributeBonuses.Add(attributeUpgrade);

            foreach (UnitData unitData in attributeUpgrade.targetUnits)
                Debug.LogFormat("{0} bonus added for {1}.", attributeUpgrade.targetAttribute.ToString(), unitData.title);
        }
        Debug.Log(this.title + "research completed.");
    }    
}
