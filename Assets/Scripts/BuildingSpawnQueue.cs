using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Audio;
using UnityEditor;
using Valve.VR.InteractionSystem;
using Swordfish.Navigation;

public class BuildingSpawnQueue : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Currently not used for anything.")]
    public RTSBuildingType buildingType;
    public int numberOfQueueSlots = 12;
    public List<RTSUnitType> unitQueueButtons;

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
    
    public GameObject resourceCostPrefab;
    private Structure structure;
    private Damageable damageable;
    public AudioClip onButtonDownAudio;
    public AudioClip onButtonUpAudio;

    public Material buttonBaseMaterial;
    public GameObject buttonLockPrefab;
    protected AudioSource audioSource;

    private RTSUnitType lastUnitQueued;

    private PlayerManager playerManager;

    void Start()
    {       
        playerManager = PlayerManager.instance;

        if (!unitSpawnPoint)
        {
            Structure structure = GetComponentInParent<Structure>();
            if (structure)
            {
                unitSpawnPoint = structure.transform;
                unitRallyWaypoint = unitSpawnPoint;
                Debug.Log("UnitSpawnPoint not set, using structure transform.", this);
            }
            else
            {
                Debug.Log("UnitSpawnPoint not set and no structure found.", this);
            }
            
            
        }

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

        QueueUnitButton firstButton = GetComponentInChildren<QueueUnitButton>(true);
        if (firstButton)
            lastUnitQueued = firstButton.unitTypeToQueue;        
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

    public bool QueueLastUnitQueued() { return QueueUnit(lastUnitQueued); }

    public bool QueueUnit(RTSUnitType unitTypeToQueue)
    {
        // TODO: Reenable this later
        // if (damageable.GetAttributePercent(Attributes.HEALTH) < 1.0f)
        //     return;
        
        if (unitSpawnQueue.Count >= structure.buildingData.maxUnitQueueSize)
            return false;

        if (structure.IsSameFaction(playerManager.factionId) &&
            !playerManager.CanQueueUnit(unitTypeToQueue))
            return false;

        UnitData unitData = GameMaster.GetUnit(unitTypeToQueue);
        playerManager.DeductUnitQueueCostFromStockpile(unitData);
        unitSpawnQueue.AddLast(unitData);
        
        // Debug.Log("Queued " + unitData.unitType);
        
        return true;
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
            playerManager.RemoveFromQueueCount(unitSpawnQueue.Last.Value.populationCost);
            unitSpawnQueue.RemoveLast();
            queueProgressImage.fillAmount = 0;
            queueProgressImage.enabled = false;
            queueProgressText.enabled = false;
            RefreshQueueImages();
        }
        else
        {
            playerManager.RemoveFromQueueCount(unitSpawnQueue.Last.Value.populationCost);
            unitSpawnQueue.RemoveLast();
        }
    }

    private void SpawnUnit()
    {
        if (unitSpawnQueue.First.Value.prefab)
        {
            GameObject unitGameObject = Instantiate(unitSpawnQueue.First.Value.prefab, unitSpawnPoint.transform.position, Quaternion.identity);
            Unit unit = unitGameObject.GetComponent<Unit>();
            unit.rtsUnitType = unitSpawnQueue.First.Value.unitType;
            unit.factionId = structure.factionId;

            // ! Dsabled, none of this works for rally points anymore.
            //unit.SyncPosition();
            //unit.GotoForced(World.ToWorldSpace(unitRallyWaypoint.position));
            //unit.LockPath();

            // Debug.Log("Spawned " + unit.rtsUnitType + ".");
        }
        else
            Debug.Log (string.Format("Spawn {0} failed. Missing prefabToSpawn.", unitSpawnQueue.First.Value.unitType));
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
            // TODO: clamping is a bandaid fix
            QueueSlotImage[Mathf.Clamp(i, 0, QueueSlotImage.Length-1)].overrideSprite = unitData.queueImage;
            i++;
        }
    }

    [ExecuteInEditMode]
    public void Generate()
    {
        Vector3 buttonsStartPosition = new Vector3(0.66f, 0.642f);
        GenerateButtons(buttonsStartPosition, -0.33f);
    }

    /// <summary>
    /// Currently limited to 5 buttons only.
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="gap"></param>
    /// <param name="orientation"></param>
    [ExecuteInEditMode]
    public void GenerateButtons(Vector3 startPosition, float gap, byte orientation = 0)
    {
        // Orientation not used currently.

        foreach(RTSUnitType unitType in unitQueueButtons)
        {
            UnitData typeData = GameMaster.GetUnit(unitType);

            // BuildingHoverButton
            GameObject buildingHoverButton = new GameObject("BuildingHoverButton", typeof(QueueUnitButton));
            buildingHoverButton.transform.parent = this.transform.GetChild(0).transform;
            buildingHoverButton.transform.localPosition = startPosition;
            buildingHoverButton.name = string.Format("Queue_{0}_Button",unitType.ToString());
            buildingHoverButton.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            QueueUnitButton queueUnitButton = buildingHoverButton.GetComponent<QueueUnitButton>();
            queueUnitButton.unitTypeToQueue = unitType;
            queueUnitButton.buttonLockedObject = buttonLockPrefab;

            // Base (child of BuildingHoverButton)
            GameObject buttonBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonBase.name = "Base";
            buttonBase.transform.parent = buildingHoverButton.transform;
            buttonBase.transform.localScale = new Vector3(0.309937507f,0.312250197f,0.0399999991f);
            buttonBase.transform.localPosition = new Vector3(0.0f, 0.0f, -0.016f);
            buttonBase.transform.GetComponent<MeshRenderer>().sharedMaterial = buttonBaseMaterial;

            // Instantiate the resource cost gameobject
            GameObject resourceCost = Instantiate(resourceCostPrefab, Vector3.zero, Quaternion.identity, buildingHoverButton.transform);
            resourceCost.transform.localPosition = new Vector3(0.0f, 0.0f, -0.014f);
            resourceCost.transform.localRotation = Quaternion.identity;

            // Popluate the resource cost prefab text objects
            BuildMenuResouceCost cost = resourceCost.GetComponent<BuildMenuResouceCost>();
            cost.woodText.text = typeData.woodCost.ToString();
            cost.goldText.text = typeData.goldCost.ToString();
            cost.grainText.text = typeData.foodCost.ToString();
            cost.stoneText.text = typeData.stoneCost.ToString();

            // Face (child of BuildingHoverButton)
            GameObject face = new GameObject("Face", typeof(Interactable), typeof(HoverButton), typeof(AudioSource));
            face.transform.parent = buildingHoverButton.transform;
            face.transform.localPosition = Vector3.zero;
            face.transform.localScale = new Vector3(0.259453088f,0.259453088f,0.0487500019f);
            HoverButton hoverButton = face.GetComponent<HoverButton>();
            hoverButton.localMoveDistance = new Vector3(0, 0, -0.3f);
            face.GetComponent<Interactable>().highlightOnHover = false;

            // Lock (child of BuildingHoverButton)
            GameObject buttonLock = Instantiate<GameObject>(buttonLockPrefab);
            buttonLock.transform.parent = buildingHoverButton.transform;
            buttonLock.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0254f);
            buttonLock.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            buttonLock.SetActive(false);

            // MovingPart (child of Face)
            GameObject buttonMovingPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonMovingPart.AddComponent<UVCubeMap>();
            buttonMovingPart.name = "MovingPart";
            buttonMovingPart.transform.SetParent(face.transform);
            buttonMovingPart.transform.localScale = new Vector3(1, 1, 1);
            buttonMovingPart.transform.localPosition = Vector3.zero;
            buttonMovingPart.transform.GetComponent<MeshRenderer>().sharedMaterial = typeData.worldButtonMaterial;

            hoverButton.movingPart = buttonMovingPart.transform;
            buildingHoverButton.transform.localRotation = Quaternion.identity;
            if (Time.time <= 0)
                Destroy(buttonBase.GetComponent<BoxCollider>());
            else
                DestroyImmediate(buttonBase.GetComponent<BoxCollider>());

            startPosition.x += gap;
        }
    }
}
