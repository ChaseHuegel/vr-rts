using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct RTSUnitTypeData
{
    public RTSUnitTypeData( RTSUnitType uType, float qTime, GameObject uPrefab, 
                            Sprite queueImage, int iGoldCost, int iStoneCost, 
                            int iGrainCost, int iWoodCost, int popCost = 1)
    {
        unitType = uType;
        queueTime = qTime;
        prefab = uPrefab;
        worldButtonImage = queueImage;
        populationCost = popCost;
        goldCost = iGoldCost;
        stoneCost = iStoneCost;
        grainCost = iGrainCost;
        woodCost = iWoodCost;

        if (queueTime < 1.0f) queueTime = 1.0f;
    }

    public RTSUnitType unitType;
    public float queueTime;
    public GameObject prefab;
    public Sprite worldButtonImage;
    public int populationCost;
    public int goldCost;
    public int stoneCost;
    public int grainCost;
    public int woodCost;
}


