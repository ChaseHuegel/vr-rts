using System.Collections;
using System.Collections.Generic;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "RTS/Units/Unit Data")]
public class UnitData : ScriptableObject
{
    public RTSUnitType unitType;
    public float queueTime;
    public GameObject prefab;
    public Sprite queueImage;
    public Material worldButtonMaterial;
    public float buildRate;
    public float foragingRate;
    public float repairRate;
    public float stoneMiningRate;
    public float goldMiningRate;
    public float farmingRate;
    public float huntingRate;
    public float lumberjackingRate;
    public float fishingRate;
    public int populationCost;
    public int goldCost;
    public int stoneCost;
    public int foodCost;
    public int woodCost;
    public int maxHitPoints;
    public int maxCargo;
    public float huntingDamage;
    public int attackRange;
    public float attackDamage;
    public float armor;
}