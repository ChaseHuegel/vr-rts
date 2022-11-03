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
}