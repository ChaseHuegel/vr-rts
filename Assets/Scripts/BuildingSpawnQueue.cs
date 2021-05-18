using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Audio;
using UnityEditor;
using Valve.VR.InteractionSystem;

public class BuildingSpawnQueue : MonoBehaviour
{
    public RTSBuildingType buildingType;
    [Header( "Unit Stuff" )]
    public Transform unitSpawnPoint;
    public Transform unitRallyWaypoint;

    // Meant to be used so units pick a random spot within the radius to
    // go to so they don't fight over a single point.
    public float unitRallyWaypointRadius;
    protected float timeElapsed = 0.0f;
    protected LinkedList<UnitData> unitSpawnQueue = new LinkedList<UnitData>();
    public TMPro.TMP_Text queueProgressText;
    public UnityEngine.UI.Image queueProgressImage;
    public UnityEngine.UI.Image[] QueueSlotImage;
    public List<RTSUnitType> unitQueueButtons;
    private Structure structure;
    private Damageable damageable;
    public AudioClip onButtonDownAudio;
    public AudioClip onButtonUpAudio;

    public Material buttonBaseMaterial;
    public Material buttonLockMaterial;
    protected AudioSource audioSource;

    void Awake()
    {
        // TODO: Pick a spot around the building and set it as the spawn point
        // when no spawn point is found. Using transform center currently.
        if (!unitSpawnPoint) 
        {
            unitSpawnPoint = transform;
            Debug.Log("UnitSpawnPoint not set.", this);
        }
    }

    void Start()
    {
        if (!(damageable = gameObject.GetComponentInParent<Damageable>()))
            Debug.Log("Missing damageable component in parent.", this);

        if (!(structure = gameObject.GetComponentInParent<Structure>()))
            Debug.Log("Missing structure component in parent.", this);

        if (!(audioSource = gameObject.GetComponentInParent<AudioSource>()))
            Debug.Log("Missing audiosource component in parent.", this);

        HoverButton[] hoverButtons = GetComponentsInChildren<HoverButton>(true);
        if (hoverButtons.Length <= 0)
            Debug.Log("No HoverButton components found in children.");
        else
            foreach(HoverButton hButton in hoverButtons)
            {
                hButton.onButtonDown.AddListener(OnButtonDown);
                hButton.onButtonUp.AddListener(OnButtonUp);
            }

        if (!(queueProgressText = GetComponentInChildren<TMPro.TextMeshPro>(true)))
            Debug.Log("queueProgressText object not found.", this);

        if (!(queueProgressImage = GetComponentInChildren<UnityEngine.UI.Image>(true)))
            Debug.Log("queueProgressImage not found.", this);
    }

    public void SetUnitRallyWaypoint(Vector3 position)
    {
        unitRallyWaypoint.transform.position = position;
    }

    void Update()
    {
        UpdateUnitSpawnQueue();
    }

    public void OnButtonDown(Hand hand)
    {
        audioSource.PlayOneShot(onButtonDownAudio);
    }

    public void OnButtonUp(Hand hand)
    {
        audioSource.PlayOneShot(onButtonUpAudio);
    }

    public void QueueUnit(RTSUnitType unitTypeToQueue)
    {
        if (!structure.IsBuilt() || damageable.GetAttributePercent(Attributes.HEALTH) < 1.0f)
            return;

        if (unitSpawnQueue.Count >= structure.rtsBuildingTypeData.maxUnitQueueSize)
            return;

        if (!PlayerManager.instance.CanQueueUnit(unitTypeToQueue))
            return;

        // if (!allowedUnitsToQueue.Contains(unitTypeToQueue))
        //     return;

        UnitData unitData = GameMaster.GetUnit(unitTypeToQueue);
        PlayerManager.instance.RemoveUnitQueueCostFromStockpile(unitData);

        unitSpawnQueue.AddLast(unitData);

        Debug.Log("Queued " + unitData.unitType);
    }

    private void UpdateUnitSpawnQueue()
    {
        if (unitSpawnQueue.Count > 0)
        {
            timeElapsed += Time.deltaTime;
            queueProgressImage.fillAmount = (timeElapsed / unitSpawnQueue.First.Value.queueTime);
            float progressPercent = UnityEngine.Mathf.Round(queueProgressImage.fillAmount * 100);
            queueProgressText.text = progressPercent.ToString() + "%";

            if (timeElapsed >= unitSpawnQueue.First.Value.queueTime)
            {
                SpawnUnit();
                timeElapsed = 0.0f;    
                unitSpawnQueue.RemoveFirst();
                queueProgressImage.fillAmount = 0;
                queueProgressImage.enabled = false;
                queueProgressText.enabled = false;
            }
            else
            {
                queueProgressImage.enabled = true;
                queueProgressText.enabled = true;
            }

            RefreshQueueImages();
        }
        else
        {
            timeElapsed = 0.0f;
        }
    }

    public void DequeueUnit()
    {
        if (unitSpawnQueue.Count <= 0)
            return;     

        else if (unitSpawnQueue.Count == 1)
        {            
            PlayerManager.instance.RemoveFromQueueCount(unitSpawnQueue.Last.Value.populationCost);
            unitSpawnQueue.RemoveLast();
            queueProgressImage.fillAmount = 0;
            queueProgressImage.enabled = false;
            queueProgressText.enabled = false;            
            RefreshQueueImages();
        }
        else
        {
            PlayerManager.instance.RemoveFromQueueCount(unitSpawnQueue.Last.Value.populationCost);
            unitSpawnQueue.RemoveLast();
        }
    }

    private void SpawnUnit()
    {
        GameObject prefabToSpawn = unitSpawnQueue.First.Value.prefab;

        if (prefabToSpawn)
        {
            GameObject unit = GameObject.Instantiate<GameObject>(unitSpawnQueue.First.Value.prefab);
            unit.transform.position = unitSpawnPoint.transform.position;
            
            Villager villager = unit.GetComponent<Villager>();

            // TODO: Not fond of this setup but it works and I don't want to dig into
            // this mess right now.
            villager.rtsUnitType = unitSpawnQueue.First.Value.unitType;
            villager.SetUnitData(unitSpawnQueue.First.Value);
            villager.SetVillagerUnitType(villager.rtsUnitTypeData.unitType);
            

            //RTSUnitType uType = unitSpawnQueue.Peek().unitType;

            Debug.Log("Spawned " + villager.rtsUnitTypeData.unitType + ".");
        }
        else
            Debug.Log ("Spawn unit failed. Missing prefabToSpawn.");
    }

    private void RefreshQueueImages()
    {
        foreach(UnityEngine.UI.Image image in QueueSlotImage)
        {
            // Clearing override sprite reenables the original
            image.overrideSprite = null;
        }

        int i = 0;
        foreach (UnitData unitData in unitSpawnQueue)
        {
            QueueSlotImage[i].overrideSprite = unitData.queueImage;
            i++;
        }
    }

    [ExecuteInEditMode]
    public void Generate()
    {
        Vector3 startPosition = new Vector3(0.66f, 0.642f);
        float gap = -0.33f;

        foreach(RTSUnitType unitType in unitQueueButtons)
        {
            UnitData typeData = GameMaster.GetUnit(unitType);

            // BuildingHoverButton
            GameObject buildingHoverButton = new GameObject("BuildingHoverButton");
            buildingHoverButton.transform.parent = this.transform;
            buildingHoverButton.transform.localPosition = startPosition;
            buildingHoverButton.transform.Rotate(0, -90, 0);
            buildingHoverButton.name = "Queue" + unitType.ToString();
            buildingHoverButton.AddComponent<QueueUnitButton>();
            buildingHoverButton.GetComponent<QueueUnitButton>().unitTypeToQueue = unitType;

            // Base (child of BuildingHoverButton)
            GameObject buttonBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonBase.name = "Base";
            buttonBase.transform.parent = buildingHoverButton.transform;
            buttonBase.transform.localPosition = new Vector3(4.45360016e-08f, 0.00300000003f, -0.0160000101f);
            buttonBase.transform.localScale = new Vector3(0.309937507f,0.312250197f,0.0399999991f);
            buttonBase.transform.Rotate(0, -90, 0);
            buttonBase.transform.GetComponent<MeshRenderer>().sharedMaterial = buttonBaseMaterial;

            // Face (child of BuildingHoverButton)
            GameObject face = new GameObject("Face", typeof(Interactable), typeof(HoverButton), typeof(AudioSource));
            face.transform.parent = buildingHoverButton.transform;
            face.transform.localPosition = Vector3.zero;
            face.transform.localScale = new Vector3(0.259453088f,0.259453088f,0.0487500019f);
            face.transform.Rotate(0, -90, 0);
            HoverButton hoverButton = face.GetComponent<HoverButton>();
            hoverButton.localMoveDistance = new Vector3(0, 0, -0.3f);
            face.GetComponent<Interactable>().highlightOnHover = false;
            
            // MovingPart (child of Face)
            GameObject buttonMovingPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonMovingPart.AddComponent<UVCubeMap>();
            buttonMovingPart.name = "MovingPart";
            buttonMovingPart.transform.SetParent(face.transform);
            buttonMovingPart.transform.localScale = new Vector3(1, 1, 1);
            buttonMovingPart.transform.localPosition = Vector3.zero;
            buttonMovingPart.transform.Rotate(0, -90, 0);
            buttonMovingPart.transform.GetComponent<MeshRenderer>().sharedMaterial = typeData.worldButtonMaterial;

            hoverButton.movingPart = buttonMovingPart.transform;

            if (Time.time <= 0)
                Destroy(buttonBase.GetComponent<BoxCollider>());
            else
                DestroyImmediate(buttonBase.GetComponent<BoxCollider>());

            startPosition.x += gap;
        }
    }
}
