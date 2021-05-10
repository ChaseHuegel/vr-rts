using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using UnityEditor;
using Swordfish;

[RequireComponent(typeof(AudioSource))]
public class TerrainBuilding : MonoBehaviour
{
    [Header( "Building Stats" )]
    public int maxUnitQueueSize = 10;

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
    public GameObject unitSpawnPoint;
    public GameObject unitRallyWaypoint;

    // Meant to be used so units pick a random spot within the radius to
    // go to so they don't fight over a single point.
    public float unitRallyWaypointRadius;
    public List<RTSUnitType> allowedUnitCreationList;
    private float timeElapsed = 0.0f;
    private Queue<RTSUnitTypeData> unitSpawnQueue = new Queue<RTSUnitTypeData>();
    public TMPro.TMP_Text queueProgressText;
    public UnityEngine.UI.Image queueProgressImage;

    public UnityEngine.UI.Image[] QueueImageObjects;   

    public BuildingSpawnHoverMenu buildingSpawnHoverMenu;

    //public Sprite emptyQueueSlotImage;

    GameObject currentPrefabUnitToSpawn;
    public float GetTimeElapsed { get { return timeElapsed; } }

    private Structure structure;
    private Damageable damageable;

    public void QueueUnit(RTSUnitType unitTypeToQueue)
    { 
        if (!structure.IsBuilt() || damageable.GetAttributePercent(Attributes.HEALTH) < 1.0f)
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
    }

    // Start is called before the first frame update
    void Start()
    {   
        damageable = gameObject.GetComponent<Damageable>();
        structure = gameObject.GetComponent<Structure>();
        audioSource = gameObject.GetComponent<AudioSource>();
        buildingSpawnHoverMenu = gameObject.GetComponentInChildren<BuildingSpawnHoverMenu>( true );
        queueProgressText = buildingSpawnHoverMenu.queueProgressText;
        queueProgressImage = buildingSpawnHoverMenu.queueProgressImage;
        buildingSpawnHoverMenu.enabled = false;

        //constructionCompletedAudio = GameMaster.GetAudio("constructionCompleted").GetClip();       
    }    

    protected void ToggleHoverMenuOnKnock()
    {
        buildingSpawnHoverMenu.gameObject.SetActive(!buildingSpawnHoverMenu.gameObject.activeSelf);
    }
    
    // protected float firstKnockTime;
    // protected float secondKnockMaxDuration = 1.0f;
    // protected bool waitingForSecondKnock;
    private void OnHandHoverBegin()
    {        
        // Check if hand pose is a fist and play knock if it is.
        audioSource.clip = GameMaster.GetAudio("knock").GetClip();
        audioSource.Play();

        ToggleHoverMenuOnKnock();

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

    // Update is called once per frame
    void Update()
    {    
        UpdateUnitSpawnQueue(); 
    }

    private void UpdateUnitSpawnQueue()
    {
        if (unitSpawnQueue.Count > 0)
        {
            timeElapsed += Time.deltaTime;
            queueProgressImage.fillAmount = (timeElapsed / unitSpawnQueue.Peek().queueTime);
            float progressPercent = UnityEngine.Mathf.Round(queueProgressImage.fillAmount * 100);
            queueProgressText.text = progressPercent.ToString() + "%";

            if (timeElapsed >= unitSpawnQueue.Peek().queueTime)
            {
                SpawnUnit();
                timeElapsed = 0.0f;
                unitSpawnQueue.Dequeue();                    
                queueProgressImage.fillAmount = 0;
                queueProgressImage.enabled = false;
                queueProgressText.enabled = false;
            }
            else
            {
                queueProgressImage.enabled = true;
                queueProgressText.enabled = true;
            }

            foreach(UnityEngine.UI.Image image in QueueImageObjects)
            {
                // Clearing override sprite reenables the original
                image.overrideSprite = null;
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
            
            Villager villager = unit.GetComponent<Villager>();
            villager.Initialize();

            //actor.SetUnitType(unitSpawnQueue.Peek().unitType);
            
            //RTSUnitType uType = unitSpawnQueue.Peek().unitType;

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
