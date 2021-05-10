using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Audio;

[System.Serializable]
public struct RTSBuildingTypeData
{
    public RTSBuildingType buildingType;
    public ResourceGatheringType dropoffTypes;
    public int goldCost;
    public int stoneCost;
    public int grainCost;
    public int woodCost;    
    public GameObject menuPreviewPrefab;
    public GameObject fadedPreviewPrefab;
    public GameObject throwablePrefab;
    public GameObject worldPrefab;
    public int populationSupported;
    public List<RTSUnitType> allowedUnitsToSpawn;
    public SoundElement constructionCompletedAudio;

}
