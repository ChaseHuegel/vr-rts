using System.Collections;
using System.Collections.Generic;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "RTS/Units/Unit Data")]
public class UnitData : TechBase
{
    [Header("Unit Settings")]
    public RTSUnitType unitType;

    [Header("Work Rates")]  
    public float buildRate;
    public float foragingRate;
    public float repairRate;
    public float stoneMiningRate;
    public float goldMiningRate;
    public float farmingRate;
    public float huntingRate;
    public float lumberjackingRate;
    public float fishingRate;

    [Header("Stats")]
    public int maximumHitPoints;
    public int maxCargo;
    public float huntingDamage;
    public int attackRange;
    public float attackDamage;
    public float armor;

    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        if (worldPrefab)
        {
            GameObject unitGameObject = Instantiate(worldPrefab, spawnQueue.UnitSpawnPoint.position, Quaternion.identity);
            UnitV2 unit = unitGameObject.GetComponent<UnitV2>();
            unit.Faction = spawnQueue.Structure.Faction;
            unit.SetUnitType(this.unitType);
            unit.IssueSmartOrder(spawnQueue.UnitRallyPointCell);
        }
        else
            Debug.Log(string.Format("Spawn {0} failed. Missing prefabToSpawn.", this.title));
    }
}
