using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSpawnHoverMenu : MonoBehaviour
{
    public TMPro.TMP_Text buildingTitleText;
    public UnityEngine.UI.Image queueProgressImage;
    public TMPro.TMP_Text queueProgressText;
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
