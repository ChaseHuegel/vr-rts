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

    [Header("Audio")]
    AudioSource audioSource;

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


    private Structure structure;
    private Damageable damageable;

    

    // Start is called before the first frame update
    void Start()
    {   
        damageable = gameObject.GetComponent<Damageable>();
        structure = gameObject.GetComponent<Structure>();
        audioSource = gameObject.GetComponent<AudioSource>();
        //queueProgressText = buildingSpawnHoverMenu.queueProgressText;
        //queueProgressImage = buildingSpawnHoverMenu.queueProgressImage; 
    }    

    // Update is called once per frame
    void Update()
    {    
        UpdateUnitSpawnQueue(); 
    }

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
            RTSUnitTypeData unitData = unitSpawnQueue.Dequeue();
            Debug.Log("Removed " + unitData + " from queue. " + unitSpawnQueue.Count + " left in queue.");
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

            villager.SetRTSUnitType(unitSpawnQueue.Peek().unitType);
            
            //RTSUnitType uType = unitSpawnQueue.Peek().unitType;

            Debug.Log("Spawned " + villager.rtsUnitType + ".");
        }
        else
            Debug.Log ("Spawn unit failed. Missing prefabToSpawn.");
    }
}
