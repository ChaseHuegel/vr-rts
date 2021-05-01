using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RTSUnitType { None, Builder, Lumberjack, GoldMiner, OreMiner, Farmer, Swordsman, };

public struct RTSUnitTypeData
{
    public RTSUnitTypeData( RTSUnitType uType, float qTime, GameObject uPrefab, Sprite worldImage, int popCost = 1)
    {
        unitType = uType;
        queueTime = qTime;
        prefab = uPrefab;
        worldButtonImage = worldImage;
        populationCost = popCost;
    }

    public RTSUnitType unitType;
    public float queueTime;
    public GameObject prefab;
    public Sprite worldButtonImage;
    public int populationCost;
}


