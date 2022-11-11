using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Wall", menuName = "RTS/Buildings/Wall Data")]
public class WallData : BuildingData
{

    [Header("Wall Prefabs")]
    public GameObject cornerPreviewPrefab;
    public GameObject cornerConstructionPrefab;
    public GameObject diagonalPreviewPrefab;
    public GameObject diagonalConstructionPrefab;


}