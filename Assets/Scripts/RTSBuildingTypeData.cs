using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RTSBuildingTypeData
{
    public RTSBuildingTypeData(RTSBuildingType uType,int iGoldCost, int iOreCost, int iGrainCost,
                                                    int iWoodCost, GameObject goWorldPrefab,
                                                    GameObject goMenuPrefab, int popSupported,
                                                    List<RTSUnitType> unitSpawnAllowed)
    {
        buildingType = uType;
        goldCost = iGoldCost;
        oreCost = iOreCost;
        grainCost = iGrainCost;
        woodCost = iWoodCost;
        worldPrefab = goWorldPrefab;
        menuPrefab = goMenuPrefab;
        populationSupported = popSupported;
        allowedUnitsToSpawn = unitSpawnAllowed;

    }

    public RTSBuildingType buildingType;
    public int goldCost;
    public int oreCost;
    public int grainCost;
    public int woodCost;
    public GameObject worldPrefab;
    public GameObject menuPrefab;
    public int populationSupported;
    public List<RTSUnitType> allowedUnitsToSpawn;

}
