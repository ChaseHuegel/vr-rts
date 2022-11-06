using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Audio;

[CreateAssetMenu(fileName = "New Building", menuName = "RTS/Buildings/Building Data")]
public class BuildingData : TechBase
{
    [Header("Building Settings")]
    public RTSBuildingType buildingType;    
    public ResourceGatheringType dropoffTypes;

    [Header("Stats")]
    public int populationSupported;
    public int maxUnitQueueSize;
    public int maximumHitPoints;
    public int garrisonCapacity;
    public int armor;

    [Header("Additional Visual Prefabs")]
    public GameObject menuPreviewPrefab;
    public GameObject fadedPreviewPrefab;
    public GameObject worldPreviewPrefab;
    public GameObject throwablePrefab;
    public GameObject constructablePrefab;
    
    [Header("Wall Specific Prefabs")]
    public GameObject diagonalWorldPrefab;
    public GameObject diagonalWorldPreviewPrefab;
    public GameObject diagonalConstructablePrefab;
    
    public int boundingDimensionX;
    public int boundingDimensionY;
    public List<RTSUnitType> allowedUnitsToSpawn;
    public SoundElement constructionCompletedAudio;
}