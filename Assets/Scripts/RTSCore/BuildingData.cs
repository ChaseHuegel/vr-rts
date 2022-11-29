using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Audio;
using System;
[CreateAssetMenu(fileName = "New Building", menuName = "RTS/Buildings/Building Data")]
public class BuildingData : TechBase
{
    [Header("Building Settings")]
    public BuildingType buildingType;    
    public ResourceGatheringType dropoffTypes;
    public int boundingDimensionX;
    public int boundingDimensionY;
    public Swordfish.Navigation.NavigationLayers allowedLayers;

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
    public GameObject constructionPrefab;

    [Header("Other Settings")]
    public SoundElement constructionCompletedAudio;

    public List<TechBase> techQueueButtons;
}

