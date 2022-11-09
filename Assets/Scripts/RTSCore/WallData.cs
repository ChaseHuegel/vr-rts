using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Wall", menuName = "RTS/Buildings/Wall Data")]
public class WallData : BuildingData
{

    [Header("Wall Prefabs")]
    public GameObject endWorldPrefab;
    public GameObject endPreviewPrefab;
    public GameObject cornerWorldPrefab;
    public GameObject cornerPreviewPrefab;
    public GameObject diagonalWorldPrefab;
    public GameObject diagonalWorldPreviewPrefab;
    public GameObject diagonalConstructionPrefab;

}
