using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class TerrainBuilding : MonoBehaviour
{
    [Header( "Construction Stages" )]
    public GameObject buildingStage0;
    public GameObject buildingStage1;
    public GameObject buildingStageFinal;
    public float stage0Duration = 10.0f;
    public float stage1Duration = 10.0f;

    [Header( "Building Stats" )]
    public int maxHealth = 100;
    public int maxUnitQueueSize = 5;

    int currentHealth;

    [Header( "Damage Effects" )]
    public GameObject buildingDamagedEffect;
    public GameObject buildingHealth75PercentEffect;
    public GameObject buildingHealth50PercentEffect;
    public GameObject buildingHealth25PercentEffect;
    public GameObject buildingDestroyedEffect;

    [Header( "Unit Stuff" )]
    public GameObject unitSpawnPoint;
    public GameObject unitRallyWaypoint;        
    public List<RTSUnitType> allowedUnitCreationList;
    
    public PlayerManager playerManager;

    private float timeElapsed = 0.0f;
    private bool constructionCompleted = false;   
    private float buildingContructionTimeTotal;
    
    private List<RTSUnitTypeData> unitSpawnQueue = new List<RTSUnitTypeData>();

    GameObject currentPrefabUnitToSpawn;    
    public float GetTimeElapsed { get { return timeElapsed; } }
    
    // TODO: Add ability for other objects to subscribe to events on
    // buildings for informational displays.
    public void QueueUnit(RTSUnitType unitTypeToQueue)
    {
        // Should check to make sure the unit type to queue is
        // a unit type this building produces at some point?
        if (constructionCompleted && currentHealth >= maxHealth)
        {
            if (unitSpawnQueue.Count < maxUnitQueueSize)
            {    
                unitSpawnQueue.Add(playerManager.FindUnitData(unitTypeToQueue));
                Debug.Log("Queued " + unitTypeToQueue.ToString() + " (" + unitSpawnQueue.Count + ")");
            }
        }

        // Set current unit type to spawn
        //currentPrefabUnitToSpawn = villagerPrefab;

        // No Queue built yet, just spawn the current unit.
        SpawnUnit();
    }

    // Start is called before the first frame update
    void Start()
    {
        playerManager = Player.instance.GetComponent<PlayerManager>();
        currentHealth = maxHealth;
        unitSpawnQueue.Capacity = maxUnitQueueSize;
        buildingContructionTimeTotal = stage0Duration + stage1Duration;
    }

    void OnTriggerEnter(Collider other)
    {
        // TerrainBuilding tbOther = other.GetComponent<TerrainBuilding>();

        // if (tbOther != null)
        // {
        //     Destroy(this);
        //     // if (timeElapsed < tbOther.TimeElapsed)
        //     // {
        //     //     Destroy(this);
        //     // }
        // }
    }

    // Update is called once per frame
    void Update()
    {
        if (constructionCompleted)
        {
            if (unitSpawnQueue.Count > 0)
            {
                timeElapsed += Time.deltaTime;
                if (timeElapsed >= unitSpawnQueue[0].queueTime)
                    SpawnUnit();
            }
        }
        else
        {
            timeElapsed += Time.deltaTime;
            
            if (timeElapsed >= buildingContructionTimeTotal)
            {
                buildingStage1.SetActive(false);
                buildingStageFinal.SetActive(true);
                constructionCompleted = true;      
                timeElapsed = 0.0f;            
            }
            else if (timeElapsed >= stage0Duration)
            {
                buildingStage0.SetActive(false);  
                buildingStage1.SetActive(true);   
                                        
            }
        }
    }

    
    private void SpawnUnit()
    {                
        GameObject unit = GameObject.Instantiate<GameObject>(unitSpawnQueue[0].prefab);
        unit.transform.position = unitSpawnPoint.transform.position;        
        unitSpawnQueue.RemoveAt(0);
    }

    
    // private IEnumerator SpawnUnit()
    // {            
    //     GameObject planting = GameObject.Instantiate<GameObject>(currentPrefabUnitToSpawn);
    //     planting.transform.position = this.transform.position;
    //     planting.transform.rotation = Quaternion.Euler(0, Random.value * 360f, 0);

    //     planting.GetComponentInChildren<MeshRenderer>().material.SetColor("_TintColor", Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));

    //     Rigidbody rigidbody = planting.GetComponent<Rigidbody>();
    //     if (rigidbody != null)
    //         rigidbody.isKinematic = true;


    //     Vector3 initialScale = Vector3.one * 0.01f;
    //     Vector3 targetScale = Vector3.one * (1 + (Random.value * 0.25f));

    //     float startTime = Time.time;
    //     float overTime = 0.5f;
    //     float endTime = startTime + overTime;

    //     while (Time.time < endTime)
    //     {
    //         planting.transform.localScale = Vector3.Slerp(initialScale, targetScale, (Time.time - startTime) / overTime);
    //         yield return null;
    //     }


    //     if (rigidbody != null)
    //         rigidbody.isKinematic = false;
    // }

}
