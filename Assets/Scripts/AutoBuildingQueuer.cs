using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBuildingQueuer : MonoBehaviour
{
    public RTSUnitType rtsUnitTypeToQueue;
    public float timeBetweenQueues;
    
    private float timer;
    
    private BuildingSpawnQueue buildingSpawnQueue;
    // Start is called before the first frame update
    void Start()
    {
        buildingSpawnQueue = GetComponentInChildren<BuildingSpawnQueue>(true);
        timer = timeBetweenQueues;
    }

    // Update is called once per frame
    void Update()
    {
        if (timer >= timeBetweenQueues)
        {
            buildingSpawnQueue.QueueUnit(rtsUnitTypeToQueue);
            timer = 0.0f;
        }
        
        timer += Time.deltaTime;
    }
}
