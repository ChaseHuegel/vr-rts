using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Audio;

[CreateAssetMenu(fileName = "New Building", menuName = "RTS/Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    public RTSBuildingType buildingType;
    public ResourceGatheringType dropoffTypes;
    public int goldCost;
    public int stoneCost;
    public int grainCost;
    public int woodCost;    
    public int populationSupported;
    public int maxUnitQueueSize;
    public int hitPoints;
    public int garrisonCapacity;
    public int armor;
    public GameObject menuPreviewPrefab;
    public GameObject fadedPreviewPrefab;
    public GameObject throwablePrefab;
    public GameObject worldPrefab;
    public int boundingDimensionX;
    public int boundingDimensionY;
    public List<RTSUnitType> allowedUnitsToSpawn;
    public SoundElement constructionCompletedAudio;
}