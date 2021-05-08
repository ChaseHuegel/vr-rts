using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using UnityEditor;

[RequireComponent(typeof(AudioSource))]
public class TerrainBuilding : MonoBehaviour
{
    [Header( "Multiplayer")]
    public int PlayerColor = 0;

    [Header( "Construction Stages" )]
    public GameObject buildingStage0;
    public GameObject buildingStage1;
    public GameObject buildingStageFinal;

    [Header( "Building Stats" )]
    public int maxHealth = 500;
    public int maxUnitQueueSize = 10;
    public int currentHealth = 1;

    [Header( "Damage Effects" )]
    public GameObject buildingDamagedEffect;
    public GameObject buildingHealth75PercentEffect;
    public GameObject buildingHealth50PercentEffect;
    public GameObject buildingHealth25PercentEffect;
    public GameObject buildingDestroyedEffect;

    [Header("Audio")]
    AudioSource audioSource;
    public AudioClip constructionCompletedAudio;
    public AudioClip unitCreatedAudio;   
    public AudioClip buildingDestroyedAudio; 

    [Header( "Unit Stuff" )]
    public int populationSupported = 1;    
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

    private Queue<RTSUnitTypeData> unitSpawnQueue = new Queue<RTSUnitTypeData>();
    public UnityEngine.UI.Text queueStatusText;
    public UnityEngine.UI.Image progressImage;

    public HealthBar buildingHealthBar;

    public UnityEngine.UI.Image[] QueueImageObjects;   
    public GameObject objectToToggleOnKnock; 

    //public Sprite emptyQueueSlotImage;

    GameObject currentPrefabUnitToSpawn;
    public float GetTimeElapsed { get { return timeElapsed; } }

    public void QueueUnit(RTSUnitType unitTypeToQueue)
    { 
        if (!constructionCompleted || currentHealth < maxHealth)
            return;

        if (unitSpawnQueue.Count >= maxUnitQueueSize)
            return;

        if (!PlayerManager.instance.CanQueueUnit(unitTypeToQueue))
            return;

        if (!allowedUnitCreationList.Contains(unitTypeToQueue))
            return;    

        RTSUnitTypeData unitData = GameMaster.Instance.FindUnitData(unitTypeToQueue);
        PlayerManager.instance.RemoveUnitQueueCostFromStockpile(unitData);                    
        unitSpawnQueue.Enqueue(unitData);
        //Debug.Log("Queued " + unitTypeToQueue.ToString() + " (" + unitSpawnQueue.Count + ")");
    }

    // Start is called before the first frame update
    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        //constructionCompletedAudio = GameMaster.GetAudio("constructionCompleted").GetClip();       

        buildingHealthBar = GetComponentInChildren<HealthBar>(true);
        if (currentHealth < maxHealth)
            buildingHealthBar.enabled = true;

        buildingStage0.SetActive(true);
        buildingStage1.SetActive(false);
        buildingStageFinal.SetActive(false);
        
        RepairDamage(0);
        RefreshHealthBar();        

        playerManager = Player.instance.GetComponent<PlayerManager>();        

        if (currentHealth < maxHealth)
        {
            ResourceManager.GetBuildAndRepairObjects().Add(this);
        }    
        
        if (dropoffType.HasFlag(ResourceGatheringType.Wood))
        {
            ResourceManager.GetWoodDropoffObjects().Add(this);
        }

        if (dropoffType.HasFlag(ResourceGatheringType.Gold))
        {
            ResourceManager.GetGoldDroppoffObjects().Add(this);
        }

        if (dropoffType.HasFlag(ResourceGatheringType.Stone))
        {
            ResourceManager.GetGoldDroppoffObjects().Add(this);
        }

        if (dropoffType.HasFlag(ResourceGatheringType.Grain))
        {
            ResourceManager.GetGrainDropoffObjects().Add(this);
        }
    }    

    protected void ToggleObjectOnKnock()
    {
        objectToToggleOnKnock.SetActive(!objectToToggleOnKnock.activeSelf);
    }
    
    // protected float firstKnockTime;
    // protected float secondKnockMaxDuration = 1.0f;
    // protected bool waitingForSecondKnock;
    private void OnHandHoverBegin()
    {        
        // Check if hand pose is a fist and play knock if it is.
        audioSource.clip = GameMaster.GetAudio("knock").GetClip();
        audioSource.Play();

        ToggleObjectOnKnock();

        // ---- 2 knocks is too unreliable at the moment and can deal with it later
        // if (waitingForSecondKnock)
        // {
        //     // This is the 2nd knock
        //     if (Time.fixedTime - firstKnockTime <= secondKnockMaxDuration)
        //     {
        //         ToggleObjectOnKnock();
        //         waitingForSecondKnock = false;
        //         Debug.Log("second " + (Time.fixedTime - firstKnockTime).ToString());
        //     }
        //     // Time windows has passed for 2nd knock
        //     else
        //     {
        //         waitingForSecondKnock = false;                
        //     }
        // }
        // // This is a new first knock
        // else
        // {
        //     firstKnockTime = Time.fixedTime;
        //     waitingForSecondKnock = true;
        //     Debug.Log("first " + firstKnockTime);
        // }

        //Debug.Log("Hover Begin");
    }

    void RefreshHealthBar()
    {
        buildingHealthBar.SetFilledAmount((float)currentHealth / (float)maxHealth);     
    }

    public void RepairDamage(int amount)
    {
        currentHealth += amount;
        RefreshHealthBar();

        if (constructionCompleted) return;

        if (currentHealth >= maxHealth)
        {
            buildingStage0.SetActive(false);
            buildingStage1.SetActive(false);
            buildingStageFinal.SetActive(true);
            constructionCompleted = true;
            audioSource.PlayOneShot(constructionCompletedAudio);
            PlayerManager.instance.IncreasePopulationLimit(populationSupported);
        }
        else if (currentHealth >= (maxHealth * 0.45f))
        {
            buildingStage0.SetActive(false);
            buildingStage1.SetActive(true);
        }    
    }

    public bool NeedsRepair()
    {
        if (currentHealth < maxHealth)
            return true;
        
        return false;
    }

    // Update is called once per frame
    void Update()
    {    
        if (currentHealth < maxHealth)
        {
            //buildingHealthBar.enabled = true;
        }
        else
        {
            buildingHealthBar.enabled = false;
            ResourceManager.GetBuildAndRepairObjects().Remove(this);
        }

        UpdateUnitSpawnQueue(); 
    }

    private void UpdateUnitSpawnQueue()
    {
        if (unitSpawnQueue.Count > 0)
        {
            timeElapsed += Time.deltaTime;
            float progress = (timeElapsed / unitSpawnQueue.Peek().queueTime) * 100;
            progress = UnityEngine.Mathf.Round(progress);
            queueStatusText.text = unitSpawnQueue.Count.ToString();
            progressImage.fillAmount = progress / 100;

            //Debug.Log(timeElapsed.ToString() + "   " + unitSpawnQueue.Peek().queueTime.ToString());
            if (timeElapsed >= unitSpawnQueue.Peek().queueTime)
            {
                SpawnUnit();
                timeElapsed = 0.0f;
                unitSpawnQueue.Dequeue();                    
                progressImage.fillAmount = 0;

                //Debug.Log("Removed unit from queue " + unitSpawnQueue.Count + " left in queue.");
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
        else
            timeElapsed = 0.0f;
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

            VillagerActor actor = unit.GetComponent<VillagerActor>();

            actor.SetUnitType(unitSpawnQueue.Peek().unitType);
            
            RTSUnitType uType = unitSpawnQueue.Peek().unitType;

            //Debug.Log("Spawned " + unit.name + ".");
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
