using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Audio;

[CreateAssetMenu(fileName = "New Unit", menuName = "RTS/Units/Unit Data")]
public class UnitData : ScriptableObject
{
    public RTSUnitType unitType;
    public float queueTime;
    public GameObject prefab;
    public Sprite queueImage;
    public Material worldButtonMaterial;
    public int populationCost;
    public int goldCost;
    public int stoneCost;
    public int grainCost;
    public int woodCost;
    public int maxHitPoints;
    public float attackDamage;
    public float armor;
}