using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RTSUnitType { Villager, Swordsman };

public class TerrainBuilding : MonoBehaviour
{
    public GameObject buildingStage0;
    public GameObject buildingStage1;
    public GameObject buildingStageFinal;
    public float stage0Duration = 10.0f;
    public float stage1Duration = 10.0f;

    public int maxHealth;

    int currentHealth;
    public GameObject buildingDamagedEffect;
    public GameObject buildingHealth75PercentEffect;
    public GameObject buildingHealth50PercentEffect;
    public GameObject buildingHealth25PercentEffect;

    public GameObject buildingDestroyedEffect;
    private float timeElapsed;
    private bool TimerStarted;   
    private float stage1EndTime;

    public RTSUnitType unitTypeToSpawn = RTSUnitType.Villager;
    public GameObject unitSpawnPoint;
    GameObject currentPrefabUnitToSpawn;
    
    public float TimeElapsed { get { return timeElapsed; } }

    public GameObject villagerPrefab;

    // TODO: Add ability for other objects to subscribe to events on
    // buildings for informational displays.
    public void QueueUnit(RTSUnitType unitTypeToQueue)
    {
        // Set current unit type to spawn
        currentPrefabUnitToSpawn = villagerPrefab;

        // No Queue built yet, just spawn the current unit.
        SpawnUnit();
    }

    // Start is called before the first frame update
    void Start()
    {
        timeElapsed = 0.0f;
        TimerStarted = true;
        stage1EndTime = stage0Duration + stage1Duration;
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
        if (TimerStarted)
        {
            timeElapsed += Time.deltaTime;
            
            if (timeElapsed >= stage1EndTime)
            {
                buildingStage1.SetActive(false);
                buildingStageFinal.SetActive(true);
                TimerStarted = false;                  
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
        GameObject unit = GameObject.Instantiate<GameObject>(currentPrefabUnitToSpawn);
        unit.transform.position = unitSpawnPoint.transform.position;        
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
