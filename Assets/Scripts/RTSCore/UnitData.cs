using System;
using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "RTS/Units/Unit Data")]
public class UnitData : TechBase
{
    //=========================================================================
    // Villager

    [Header("Villager Work Rates")]  
    public float buildRate;
    public float foragingRate;
    public float healRate;
    public float stoneMiningRate;
    public float goldMiningRate;
    public float farmingRate;
    public float huntingRate;
    public float lumberjackingRate;
    public float fishingRate;
    public float collectRate;

    [Header("Villager Stats")]
    public int maxCargo = 10;
    public int maxWoodCargo = 10;
    public int maxStoneCargo = 10;
    public int maxGoldCargo = 10;
    public int maxFoodCargo = 10;
    public int huntingDamage = 1;

    //=========================================================================
    // Unit
    [Header("Unit Stats")]
    public float movementSpeed = 0.3f;
    public int maximumHitPoints;
    public float armor;
    public int attackRange;
    public float attackSpeed = 1.0f;
    public DamageType damageType = DamageType.BLUDGEONING;
    public float attackDamage;
        
    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        if (worldPrefab)
        {
            GameObject unitGameObject = Instantiate(worldPrefab, spawnQueue.UnitSpawnPoint.position, Quaternion.identity);
            UnitV2 unit = unitGameObject.GetComponent<UnitV2>();
            unit.Faction = spawnQueue.Structure.Faction;
            unit.unitData = this;
            unit.IssueSmartOrder(spawnQueue.UnitRallyPointCell);
        }
        else
            Debug.Log(string.Format("Spawn {0} failed. Missing prefabToSpawn.", this.title));
    }
}