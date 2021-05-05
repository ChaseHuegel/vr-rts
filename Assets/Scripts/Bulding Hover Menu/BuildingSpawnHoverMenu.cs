using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSpawnHoverMenu : MonoBehaviour
{
    TerrainBuilding terrainBuilding;

    void Start()
    {
        terrainBuilding = GetComponentInParent<TerrainBuilding>();
    }
    public void QueueUnit(int queueUnit)
    {

        RTSUnitType unitType = (RTSUnitType)queueUnit;
        terrainBuilding.QueueUnit(unitType);        
    }

    public void DequeueUnit()
    {
        terrainBuilding.RemoveLastUnitFromQueue();
    }
}
