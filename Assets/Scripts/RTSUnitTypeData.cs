using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum RTSUnitType { None, Builder, Lumberjack, Miner, Farmer, Swordsman, };

public struct RTSUnitTypeData
{
    public RTSUnitTypeData( RTSUnitType uType, float qTime, GameObject uPrefab, Sprite worldImage)
    {
        unitType = uType;
        queueTime = qTime;
        prefab = uPrefab;
        worldButtonImage = worldImage;
    }

    public RTSUnitType unitType;
    public float queueTime;
    public GameObject prefab;
    public Sprite worldButtonImage;
}


