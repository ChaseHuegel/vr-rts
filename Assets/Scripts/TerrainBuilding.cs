using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class TerrainBuilding : MonoBehaviour
{
    [Header( "Multiplayer")]
    public int PlayerColor = 0;

    [Header( "Construction Stages" )]
    public GameObject buildingStage0;
    public GameObject buildingStage1;
    public GameObject buildingStageFinal;
    public float stage0Duration = 10.0f;
    public float stage1Duration = 10.0f;

    [Header( "Building Stats" )]
    public int maxHealth = 100;
    public int maxUnitQueueSize = 10;

    int currentHealth;

    [Header( "Damage Effects" )]
    public GameObject buildingDamagedEffect;
    public GameObject buildingHealth75PercentEffect;
    public GameObject buildingHealth50PercentEffect;
    public GameObject buildingHealth25PercentEffect;
    public GameObject buildingDestroyedEffect;

    [Header( "Unit Stuff" )]
    public ResourceGatheringType dropoffType = ResourceGatheringType.None;
    public GameObject unitSpawnPoint;
    public GameObject unitRallyWaypoint;

    // Meant to be used so units pick a random spot within the radius to
    // go to so they don't fight over a single point.
    public float unitRallyWaypointRadius;
    public List<RTSUnitType> allowedUnitCreationList;

    public PlayerManager playerManager;

    private float timeElapsed = 0.0f;
    private bool constructionCompleted = false;
    private float buildingContructionTimeTotal;

    private Queue<RTSUnitTypeData> unitSpawnQueue = new Queue<RTSUnitTypeData>();

    public UnityEngine.UI.Text queueStatusText;

    public UnityEngine.UI.Image progressImage;

    public UnityEngine.UI.Image[] QueueImageObjects;

    //public Sprite emptyQueueSlotImage;

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
                unitSpawnQueue.Enqueue(playerManager.FindUnitData(unitTypeToQueue));
                Debug.Log("Queued " + unitTypeToQueue.ToString() + " (" + unitSpawnQueue.Count + ")");
            }
        }

        // Set current unit type to spawn
        //currentPrefabUnitToSpawn = villagerPrefab;

        // No Queue built yet, just spawn the current unit.
        //SpawnUnit();
    }

    // Start is called before the first frame update
    void Start()
    {
        playerManager = Player.instance.GetComponent<PlayerManager>();
        currentHealth = maxHealth;
        buildingContructionTimeTotal = stage0Duration + stage1Duration;

        switch (dropoffType)
        {
            case ResourceGatheringType.Wood:
                ResourceManager.GetLumberMills().Add(this);
                break;

            case ResourceGatheringType.Gold:
                ResourceManager.GetTownHalls().Add(this);
                break;

            case ResourceGatheringType.Ore:
                ResourceManager.GetTownHalls().Add(this);
                break;

            case ResourceGatheringType.Grain:
                ResourceManager.GetGranaries().Add(this);
                break;
        }
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
                float progress = (timeElapsed / unitSpawnQueue.Peek().queueTime) * 100;
                progress = UnityEngine.Mathf.Round(progress);
                queueStatusText.text = unitSpawnQueue.Count.ToString();// progress.ToString() + "%";
                progressImage.fillAmount = progress / 100;

                if (timeElapsed >= unitSpawnQueue.Peek().queueTime)
                {
                    SpawnUnit();
                    timeElapsed = 0.0f;
                    unitSpawnQueue.Dequeue();
                    Debug.Log("Removed unit from queue " + unitSpawnQueue.Count + " left in queue.");
                    progressImage.fillAmount = 0;
                }

                foreach(UnityEngine.UI.Image image in QueueImageObjects)
                {
                    image.overrideSprite = null;// emptyQueueSlotImage;
                }

                int i = 0;
                foreach (RTSUnitTypeData unitData in unitSpawnQueue)
                {
                    QueueImageObjects[i].overrideSprite = unitData.worldButtonImage;
                    i++;
                }
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

    public void RemoveLastUnitFromQueue()
    {
        if (unitSpawnQueue.Count > 0)
        {
            unitSpawnQueue.Dequeue();
            Debug.Log("Removed unit from queue " + unitSpawnQueue.Count + " left in queue.");
        }
    }

    private void SpawnUnit()
    {
        GameObject prefabToSpawn = unitSpawnQueue.Peek().prefab;

        if (prefabToSpawn)
        {
            GameObject unit = GameObject.Instantiate<GameObject>(unitSpawnQueue.Peek().prefab);
            unit.transform.position = unitSpawnPoint.transform.position;
            Debug.Log("Spawned " + unit.name + ".");
        }
        else
            Debug.Log ("Spawn unit failed. Missing prefabToSpawn");
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
