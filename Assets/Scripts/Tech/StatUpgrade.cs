using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Swordfish;

[CreateAssetMenu(fileName = "Stat Upgrade", menuName = "RTS/Tech/Stat Upgrade")]
public class StatUpgrade : TechBase
{
    [Flags]
    public enum UpgradeType
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
        COLLECT_RATE = 512,
        HEAL_RATE = 1024,
    }

    [Header("Upgrades")]
    public List<UnitData> targetUnitTypes;    
    public DamageType damageTypes;
    public UpgradeType upgradeTypes;
    public float amount;

    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);
    }
}
