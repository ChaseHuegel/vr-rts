using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RTSUnitType { Villager, Swordsman };

public struct RTSUnitTypeData
{
    public RTSUnitTypeData( RTSUnitType uType, float qTime, GameObject uPrefab)
    {
        unitType = uType;
        queueTime = qTime;
        prefab = uPrefab;
    }

    public RTSUnitType unitType;
    public float queueTime;
    public GameObject prefab;
}


