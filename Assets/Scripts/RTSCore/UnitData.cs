using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "RTS/Units/Unit Data")]
public class UnitData : TechBase
{
    [Header("Work Rates")]  
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

    [Header("Stats")]
    public float movementSpeed = 0.3f;
    public int maximumHitPoints;
    public float armor;
    public int maxCargo;
    public int attackRange;
    public float attackSpeed = 1.0f;
    public DamageType damageType = DamageType.BLUDGEONING;
    public float attackDamage;
    public float huntingDamage;
    
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