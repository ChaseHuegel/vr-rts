
using System;
using System.Collections.Generic;
using System.Linq;
using Swordfish;
using Swordfish.Audio;
using Swordfish.Navigation;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
public class InteractionPointer : MonoBehaviour
{
    //=========================================================================
    [Header("Actions")]
    public SteamVR_Action_Single uiInteractAction = SteamVR_Input.GetAction<SteamVR_Action_Single>("InteractUI");
    public SteamVR_Action_Boolean showPointerAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("ShowPointer");
    public SteamVR_Action_Boolean selectAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Select");
    public SteamVR_Action_Boolean cancelAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Cancel");
    public SteamVR_Action_Boolean queueAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Queue");
    public SteamVR_Action_Boolean dequeueAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Dequeue");
    public SteamVR_Action_Boolean rotateBuildingClockwiseAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("RotateBuildingClockwise");
    public SteamVR_Action_Boolean rotateBuildingCounterclockwiseAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("RotateBuildingCounterclockwise");
    public SteamVR_Action_Boolean teleportAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport");
    public SteamVR_Action_Boolean grabGripAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
    
    //=========================================================================
    public GameObject pointerAttachmentPoint;
    public LayerMask traceLayerMask;
    public LayerMask allowedPlacementLayers;
    public float floorFixupMaximumTraceDistance = 1.0f;
    public Material pointVisibleMaterial;
    public Transform destinationReticleTransform;
    public Transform invalidReticleTransform;
    public Color pointerValidColor;
    public Color pointerInvalidColor;
    public float arcDistance = 10.0f;

    //=========================================================================
    public Material buildingPlacementInvalidMat;
    private Material buildingPlacementCachedMat;
    private LineRenderer pointerLineRenderer;
    private LineRenderer[] unitSelectionLineRenderers;
    private GameObject pointerObject;
    private Transform pointerStartTransform;
    public float teleportFadeTime = 0.1f;
    public Hand pointerHand = null;
    private Player player = null;
    private TeleportArc teleportArc = null;
    public bool showPointerArc = false;
    private PointerInteractable pointedAtPointerInteractable;
    private SpawnQueue spawnQueue;
    private List<ActorV2> selectedActors;
    private Vector3 pointedAtPosition;
    private Vector3 prevPointedAtPosition;
    private float pointerShowStartTime = 0.0f;
    private float pointerHideStartTime = 0.0f;
    // private bool meshFading = false;
    private float fullTintAlpha;
    // private float invalidReticleMinScale = 0.2f;
    // private float invalidReticleMaxScale = 1.0f;
    // private float loopingAudioMaxVolume = 0.0f;
    private AllowTeleportWhileAttachedToHand allowTeleportWhileAttached = null;
    //private Vector3 startingFeetOffset = Vector3.zero;
    //private bool movedFeetFarEnough = false;
    public Hand handReticle;
    public bool useHandAsReticle;
    private bool teleporting = false;
    private float currentFadeTime = 0.0f;
    public GameObject wayPointReticle;
    private Resource pointedAtResource;
    private Vector3 rallyWaypointArcStartPosition;
    private GameObject rallyPointObject;
    private float triggerAddToSelectionThreshold = 0.85f;

    // Cache value
    private int maxUnitSelectionCount;

    // Cache value
    private Faction faction;
    private PlayerManager playerManager;

    //=========================================================================
    // Modes
    private bool isInUnitSelectionMode;
    private bool isInBuildingPlacementMode;
    private bool isInWallPlacementMode;
    private bool isSettingRallyPoint;

    //=========================================================================
    [Header("Building Placement")]
    public float buildingPlacementRotationIncrement = 90.0f;
    public float wallPlacementRotationIncrement = 45f;
    private GameObject buildingPlacementPreviewObject;
    private float lastBuildingRotation;
    private BuildingData placementBuildingData;

    //=========================================================================
    // Wall Related
    private GameObject wallPlacementPreviewAnchor;
    private GameObject wallPlacementPreviewCornerObject;

    //=========================================================================
    // Cached wall objects
    private WallData currentWallData;
    private List<GameObject> wallPreviewDiagonalSegments = new List<GameObject>();
    private List<GameObject> wallPreviewCornerSegments = new List<GameObject>();
    private List<GameObject> wallPreviewStraightSegments = new List<GameObject>();
    private Swordfish.Coord2D lastPreviewPointerPosition;

    //=========================================================================
    private static InteractionPointer _instance;
    public static InteractionPointer instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<InteractionPointer>();
            }

            return _instance;
        }
    }

    //=========================================================================
    void Awake()
    {
        _instance = this;

        pointerLineRenderer = GetComponentInChildren<LineRenderer>();
        pointerObject = pointerLineRenderer.gameObject;
        handReticle.enabled = useHandAsReticle;

#if UNITY_URP
		fullTintAlpha = 0.5f;
#else
        int tintColorID = Shader.PropertyToID("_TintColor");
        fullTintAlpha = pointVisibleMaterial.GetColor(tintColorID).a;
#endif
        teleportArc = GetComponent<TeleportArc>();
        teleportArc.traceLayerMask = traceLayerMask;
    }

    //=========================================================================
    void Start()
    {
        HookIntoEvents();

        playerManager = PlayerManager.Instance;
        //interactableObjects = GameObject.FindObjectsOfType<PointerInteractable>();
        selectedActors = new List<ActorV2>();

        // Cache some values
        maxUnitSelectionCount = GameMaster.Instance.maximumUnitSelectionCount;
        faction = playerManager.faction;
        
        InitializeUnitSelectionLineRenderers();

        player = Valve.VR.InteractionSystem.Player.instance;
        if (player == null)
        {
            Debug.LogError("<b>[SteamVR Interaction]</b> InteractionPointer: No Player instance found in map.", this);
            Destroy(this.gameObject);
            return;
        }

        rallyPointObject = wayPointReticle.transform.GetChild(0).gameObject;

        // Initialize reticle
        //ShowPointer();
        pointerStartTransform = pointerAttachmentPoint.transform;
    }

    // Setup LineRenderers for unit selection
    private void InitializeUnitSelectionLineRenderers()
    {
        unitSelectionLineRenderers = new LineRenderer[maxUnitSelectionCount];
        for (int i = 0; i < maxUnitSelectionCount; i++)
        {
            unitSelectionLineRenderers[i] = Instantiate(pointerLineRenderer, this.transform);
            unitSelectionLineRenderers[i].enabled = false;
        }
    }

    public void DisableInteraction()
    {
        this.enabled = false;
    }

    public void EnableInteraction()
    {
        this.enabled = true;
    }
 
    //=========================================================================
    void Update()
    {
        UpdatePointer();

        foreach (Hand hand in player.hands)
        {
            if (isInUnitSelectionMode == true && hand.currentAttachedObject != null)
            {
                EndUnitSelectionMode();
                return;
            }
            
            // if (showPointerAction.GetState(SteamVR_Input_Sources.Any) == true &&
            //     !isInUnitSelectionMode &&
            //     !hand.hoveringInteractable &&
            //     !hand.currentAttachedObject)
            //     ShowPointer();
            // else
            //     HidePointer();

            if (WasTeleportButtonReleased(hand))
            {
                if (pointerHand == hand)
                    pointerHand = null;
                
                TryTeleportPlayer();
                HidePointer();                
            }
            else if (WasTeleportButtonPressed(hand))
            {
                pointerHand = hand;
                ShowPointer();
            }
            else if (WasInteractButtonReleased(hand))
            {
                ExecuteInteraction();
            }
            else if (WasInteractButtonPressed(hand))
            {
                StartInteraction(hand);
            }
            else if (WasCancelButtonPressed(hand))
            {
                pointerHand = hand;
                if (isInUnitSelectionMode)
                    EndUnitSelectionMode();
                else if (isInBuildingPlacementMode)
                    EndBuildingPlacementMode();
                else if (pointedAtPointerInteractable)
                {
                    WallGate wallGate = pointedAtPointerInteractable.GetComponent<WallGate>();
                    if (wallGate)
                    {
                        wallGate.CloseDoors();
                        continue;
                    }
                }
            }
            else if (WasCancelButtonReleased(hand))
            {
                if (pointerHand == hand)
                    pointerHand = null;
            }
            else if (WasSelectButtonPressed(hand))
            {
                pointerHand = hand;
                if (pointedAtPointerInteractable)
                {
                    BuildingInteractionPanel buildingInteractionPanel = pointedAtPointerInteractable.GetComponentInChildren<BuildingInteractionPanel>();
                    if (buildingInteractionPanel)
                    {
                        buildingInteractionPanel.Toggle();
                        continue;
                    }                    
                }          
            }            
            else if (WasSelectButtonReleased(hand))
            {
                if (pointerHand == hand)
                    pointerHand = null;
            }

            else if (isInBuildingPlacementMode)
            {
                // TODO: Should gates snap to nearby walls without having to be exactly lined
                // TODO: up with the wall?
                float rotationIncrement = buildingPlacementRotationIncrement;
                if (placementBuildingData.buildingType == RTSBuildingType.Wood_Wall_Gate ||
                    placementBuildingData.buildingType == RTSBuildingType.Stone_Wall_Gate)
                    rotationIncrement = wallPlacementRotationIncrement;

                if (WasRotateClockwiseButtonPressed(hand))
                    buildingPlacementPreviewObject.transform.Rotate(0.0f, -rotationIncrement, 0.0f);

                if (WasRotateCounterclockwiseButtonPressed(hand))
                    buildingPlacementPreviewObject.transform.Rotate(0.0f, rotationIncrement, 0.0f);
            }
            
            /* if (WasQueueButtonPressed(hand))
                newPointerHand = hand;

            if (WasQueueButtonReleased(hand) && pointedAtPointerInteractable)
            {
                if (pointerHand == hand)
                {
                    SpawnQueue buildingSpawnQueue = pointedAtPointerInteractable.GetComponentInChildren<SpawnQueue>();
                    if (buildingSpawnQueue && buildingSpawnQueue.QueueLastUnitQueued())
                        PlayAudioClip(headAudioSource, queueSuccessSound);
                    else
                        PlayAudioClip(headAudioSource, queueFailedSound);
                }
            }

            if (WasDequeueButtonPressed(hand))
                newPointerHand = hand;

            if (WasDequeueButtonReleased(hand) && pointedAtPointerInteractable)
            {
                if (pointerHand == hand)
                {
                    SpawnQueue buildingSpawnQueue = pointedAtPointerInteractable.GetComponentInChildren<SpawnQueue>();
                    if (buildingSpawnQueue)
                    {
                        buildingSpawnQueue.DequeueUnit();
                        PlayAudioClip(headAudioSource, dequeueSound);
                    }
                }
            } */
        }
    }

    private bool WasInteractButtonPressed(Hand hand)
    {
        if (CanInteract(hand) && pointerHand == null)
        {
            if (uiInteractAction.GetAxis(hand.handType) > 0)
            {
                pointerHand = hand;
                return true;
            }
        }

        return false;
    }

    private bool WasInteractButtonReleased(Hand hand)
    {
        if (CanInteract(hand) && pointerHand != null && pointerHand == hand)
        {
            if (uiInteractAction.GetAxis(hand.handType) <= 0)
            {
                pointerHand = null;
                return true;
            }
        }

        return false;
    }

    private bool WasGrabGripPressed(Hand hand)
    {
        if (CanInteract(hand))
            return grabGripAction.GetStateDown(hand.handType);

        return false;
    }

    private bool WasGrabGripReleased(Hand hand)
    {
        if (CanInteract(hand))
            return grabGripAction.GetStateUp(hand.handType);

        return false;
    }

    /// <summary>
    /// Starts intaraction with the button bound to InteractUI.
    /// </summary>
    /// <param name="hand">The hand that pressed the InteractUI button.</param>
    private void StartInteraction(Hand hand)
    {
        if (isInWallPlacementMode)
        {
            if (!wallPlacementPreviewAnchor)
            {
                wallPlacementPreviewAnchor = Instantiate(buildingPlacementPreviewObject, buildingPlacementPreviewObject.transform.position, Quaternion.identity);
                //wallPlacementPreviewAnchor.transform.Rotate(0, lastBuildingRotation, 0);
            }
            
            currentWallData = (WallData)placementBuildingData;
        }
        else if (pointedAtPointerInteractable != null)
        {
            spawnQueue = pointedAtPointerInteractable.GetComponentInChildren<SpawnQueue>();

            if (spawnQueue && spawnQueue.enabled && !isSettingRallyPoint)
            {
                rallyWaypointArcStartPosition = pointedAtPointerInteractable.transform.position;
                isSettingRallyPoint = true;
                wayPointReticle.SetActive(true);
                return;
            }

            ActorV2 hoveredActor = pointedAtPointerInteractable.GetComponent<ActorV2>();
            if (hoveredActor &&
                !isInUnitSelectionMode &&
                hoveredActor.Faction.IsSameFaction(faction))
            {
                selectedActors.Add(hoveredActor);
                isInUnitSelectionMode = true;
                return;
            }

            QueueUnitButton queueUnitButton = pointedAtPointerInteractable.GetComponentInChildren<QueueUnitButton>();
            if (queueUnitButton)
            {
                spawnQueue = pointedAtPointerInteractable.GetComponentInParent<SpawnQueue>();
                spawnQueue.QueueTech(queueUnitButton.techToQueue);
                pointedAtPointerInteractable.GetComponentInChildren<HoverButton>().onButtonDown.Invoke(hand);
                return;
            }

            // TODO: This should be used for queueing as well...
            HoverButton hoverButton = pointedAtPointerInteractable.GetComponentInChildren<HoverButton>();
            if (hoverButton)
            {                
                hoverButton.onButtonDown.Invoke(hand);
                return;
            }

            WallGate wallGate = pointedAtPointerInteractable.GetComponent<WallGate>();
            if (wallGate)
            {
                wallGate.ToggleDoors();
            }
        }
        // Start unit selection mode if no interactible object is pointed at.
        else if (pointedAtPointerInteractable == null)
        {
            isInUnitSelectionMode = true;
        }
    }

    /// <summary>
    /// Executes an interaction. The interation has been accepted and this function
    /// completes it, in contrast to a cancelled interaction.
    /// </summary>
    private void ExecuteInteraction()
    {
        if (isInWallPlacementMode)
        {
            if (playerManager.CanAffordTech(currentWallData))
                InstantiateWallConstructionSegments();
            else
                EndWallPlacementMode();

        }
        else if (isInBuildingPlacementMode)
        {
            isInBuildingPlacementMode = false;
            bool cellsOccupied = CellsOccupied(buildingPlacementPreviewObject.transform.position, placementBuildingData.boundingDimensionX, placementBuildingData.boundingDimensionY);

            if (cellsOccupied)
            {
                if (placementBuildingData.constructionPrefab.GetComponent<Constructible>().ClearExistingWalls == true)
                {
                    Cell currentCell = World.at(World.ToWorldCoord(buildingPlacementPreviewObject.transform.position));
                    if (currentCell.GetFirstOccupant<Body>().GetComponent<WallSegment>())
                    {
                        PlayerManager.Instance.PlayBuildingPlacementAllowedAudio();
                        GameObject gameObject = Instantiate(placementBuildingData.constructionPrefab, buildingPlacementPreviewObject.transform.position, buildingPlacementPreviewObject.transform.rotation);
                        gameObject.GetComponent<Constructible>().Faction = this.faction;
                        PlayerManager.Instance.DeductTechResourceCost(placementBuildingData);
                    }
                }
                else
                    PlayerManager.Instance.PlayBuildingPlacementDeniedAudio();
            }
            else if (!cellsOccupied)
            {
                PlayerManager.Instance.PlayBuildingPlacementAllowedAudio();
                GameObject gameObject = Instantiate(placementBuildingData.constructionPrefab, buildingPlacementPreviewObject.transform.position, buildingPlacementPreviewObject.transform.rotation);
                gameObject.GetComponent<Constructible>().Faction = this.faction;
                PlayerManager.Instance.DeductTechResourceCost(placementBuildingData);
            }

            //lastBuildingRotation = buildingPlacementPreviewObject.transform.localRotation.eulerAngles.z;
            Destroy(buildingPlacementPreviewObject);
            buildingPlacementPreviewObject = null;

            // Reenable snap turn since it's turned off for rotating building using sticks
            // Should be unnecessary with different steam profiles for different action sets if we decide
            // to go down that road. 
            SetSnapTurnEnabled(true, true);
        }

        else if (isSettingRallyPoint)
        {
            // TODO: Draw line to rally point.
            spawnQueue.SetUnitRallyPointPosition(wayPointReticle.transform.position);
            wayPointReticle.SetActive(false);

            GameObject gameObject = Instantiate<GameObject>(rallyPointObject, rallyPointObject.transform.position, rallyPointObject.transform.rotation);
            gameObject.transform.localScale = rallyPointObject.transform.lossyScale;
            gameObject.GetComponentInChildren<Animator>().Play("deploy");
            Destroy(gameObject, 2.0f);

            PlayerManager.Instance.PlaySetRallyPointSound();
            spawnQueue = null;
            isSettingRallyPoint = false;
            pointerLineRenderer.enabled = false;

            return;
        }
        else if (selectedActors.Count > 0)
        {
            foreach (ActorV2 actor in selectedActors)
            {
                Body body = pointedAtPointerInteractable?.GetComponents<Body>().FirstOrDefault(x => x.enabled);
                if (pointedAtPointerInteractable && body)
                    actor.IssueTargetedOrder(body);
                else
                    actor.IssueGoToOrder(World.ToWorldCoord(pointedAtPosition));
            }

            // Cleanup
            EndUnitSelectionMode();
            pointedAtResource = null;
        }
        else
        {
            isInUnitSelectionMode = false;
        }
    }

    private void InstantiateWallConstructionSegments()
    {
        // First segment
        Instantiate(currentWallData.cornerConstructionPrefab, wallPlacementPreviewAnchor.transform.position, gameObject.transform.rotation);

        // Last segment
        Instantiate(currentWallData.cornerConstructionPrefab, buildingPlacementPreviewObject.transform.position, gameObject.transform.rotation);


        foreach (GameObject gameObject in wallPreviewStraightSegments)
        {
            Cell currentCell = World.at(World.ToWorldCoord(gameObject.transform.position));

            if (!currentCell.occupied)
                Instantiate(currentWallData.constructionPrefab, gameObject.transform.position, gameObject.transform.rotation);
            else if (IsCellOccupiedByConstructionWall(currentCell, true) || IsCellOccupiedByExistingWall(currentCell, true))
                Instantiate(currentWallData.cornerConstructionPrefab, gameObject.transform.position, gameObject.transform.rotation);
        }

        foreach (GameObject gameObject in wallPreviewDiagonalSegments)
        {
            Cell currentCell = World.at(World.ToWorldCoord(gameObject.transform.position));

            if (!currentCell.occupied)
                Instantiate(currentWallData.diagonalConstructionPrefab, gameObject.transform.position, gameObject.transform.rotation);
            else if (IsCellOccupiedByConstructionWall(currentCell, true) || IsCellOccupiedByExistingWall(currentCell, true))
                Instantiate(currentWallData.cornerConstructionPrefab, gameObject.transform.position, gameObject.transform.rotation);
        }

        foreach (GameObject gameObject in wallPreviewCornerSegments)
        {
            Cell currentCell = World.at(World.ToWorldCoord(gameObject.transform.position));

            if (!currentCell.occupied)
                Instantiate(currentWallData.cornerConstructionPrefab, gameObject.transform.position, gameObject.transform.rotation);
            else if (IsCellOccupiedByConstructionWall(currentCell, true) || IsCellOccupiedByExistingWall(currentCell, true))
                Instantiate(currentWallData.cornerConstructionPrefab, gameObject.transform.position, gameObject.transform.rotation);
        }

        EndWallPlacementMode();
    }

    private bool CellsOccupied(Vector3 position, int dimensionX, int dimensionY)
    {
        Cell cell = World.at(World.ToWorldCoord(position));

        int startX = cell.x - dimensionX / 2;
        int startY = cell.y - dimensionY / 2;
        int endX = startX + dimensionX;
        int endY = startY + dimensionY;

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                Cell curCell = World.at(x, y);
                if (curCell.occupied)
                    return true;
            }
        }
        return false;
    }

    private void EndBuildingPlacementMode()
    {
        isInBuildingPlacementMode = false;
        Destroy(buildingPlacementPreviewObject);
        buildingPlacementPreviewObject = null;
        buildingPlacementCachedMat = null;

        // TODO: Restore resources to player?

        SetSnapTurnEnabled(true, true);
    }

    private void ClearWallPreviewSections()
    {
        GameObject[] walls = wallPreviewCornerSegments.ToArray();
        for (int i = 0; i < walls.Length; i++)
            Destroy(walls[i]);

        wallPreviewCornerSegments.Clear();

        walls = wallPreviewDiagonalSegments.ToArray();
        for (int i = 0; i < walls.Length; i++)
            Destroy(walls[i]);

        wallPreviewDiagonalSegments.Clear();

        walls = wallPreviewStraightSegments.ToArray();
        for (int i = 0; i < walls.Length; i++)
            Destroy(walls[i]);

        wallPreviewStraightSegments.Clear();
        
    }

    private void DrawWallPreviewSegments()
    {
        ClearWallPreviewSections();

        // Start position, will be corner wall segment
        Vector3 anchorPosition = wallPlacementPreviewAnchor.transform.position;

        // Reticle position, will also be a corner
        Vector3 reticlePosition = buildingPlacementPreviewObject.transform.position;

        // ! This is to reduce DrawWallPreview calls, commented out for testing.
        // if (lastPreviewPointerPosition == World.ToWorldCoord(reticlePosition))
        //     return;
        // lastPreviewPointerPosition = World.ToWorldCoord(reticlePosition);

        // Grid unit * wall bounding dimension
        float wallWorldLength = 0.125f * 1.0f;

        // Vector from start position to reticle position
        Vector3 dir = (reticlePosition - anchorPosition).normalized;

        Swordfish.Coord2D startCoord = World.ToWorldCoord(anchorPosition);
        Swordfish.Coord2D endCoord = World.ToWorldCoord(reticlePosition);

        // Track the position of the previous wall segment so we can decide
        // the rotation of the next wall segment in relation to the previous
        // wall segment.
        Swordfish.Coord2D previousSegmentCoord = startCoord;
        Swordfish.Coord2D currentSegmentCoord = World.ToWorldCoord(anchorPosition + (dir * wallWorldLength));
        Swordfish.Coord2D coordDistance = endCoord - startCoord;

        GameObject obj = null;

        int wallSizeX = Mathf.Abs(coordDistance.x);
        int wallSizeY = Mathf.Abs(coordDistance.y);
        int directionX = (int)Mathf.Sign(coordDistance.x);
        int directionY = (int)Mathf.Sign(coordDistance.y);

        // Diagonal
        if (wallSizeX == wallSizeY && wallSizeX > 1)
        {
            // Create 45 degree segments
            for (int i = 1; i < wallSizeX; ++i)
            {
                if (directionX != directionY)
                {
                    obj = Instantiate(currentWallData.diagonalPreviewPrefab, World.ToTransformSpace(currentSegmentCoord), Quaternion.AngleAxis(90.0f, Vector3.up));
                    wallPreviewDiagonalSegments.Add(obj);
                }    
                else
                {
                    obj = Instantiate(currentWallData.diagonalPreviewPrefab, World.ToTransformSpace(currentSegmentCoord), Quaternion.identity);
                    wallPreviewDiagonalSegments.Add(obj);
                }

                previousSegmentCoord = currentSegmentCoord;
                currentSegmentCoord.x += 1 * directionX;
                currentSegmentCoord.y += 1 * directionY;

                // Preview objects don't have buildingDimensions on the object
                // so we have to snap ourselves.
                if (obj)
                    HardSnapToGrid(obj.transform, 1, 1, true);
            }
        }

        // Straight
        else
        {
            //-----------------------------------------------------------------
            // East - West (X axis)

            // Reset to start position
            currentSegmentCoord = startCoord;

            for (int i = 0; i < wallSizeX - 1; ++i)
            {
                currentSegmentCoord.x += 1 * directionX;

                Cell currentCell = World.at(currentSegmentCoord);

                // Cell empty
                if (!currentCell.occupied)
                {
                    obj = Instantiate(currentWallData.worldPreviewPrefab, World.ToTransformSpace(currentSegmentCoord), Quaternion.AngleAxis(90, Vector3.up));
                    wallPreviewStraightSegments.Add(obj);
                }
                else if (IsCellOccupiedByExistingWall(currentCell) || IsCellOccupiedByConstructionWall(currentCell))
                {
                    obj = Instantiate(currentWallData.cornerPreviewPrefab, World.ToTransformSpace(currentSegmentCoord), Quaternion.identity);
                    wallPreviewCornerSegments.Add(obj);
                }

                // Preview objects don't have buildingDimensions on the object
                // so we have to snap ourselves.
                if (obj)
                    HardSnapToGrid(obj.transform, 1, 1, true);
            }

            // Not a straight wall, need a corner
            if (endCoord.x != currentSegmentCoord.x && endCoord.y != currentSegmentCoord.y)
            {
                currentSegmentCoord.x += 1 * (int)Mathf.Sign(coordDistance.x);

                Cell currentCell = World.at(currentSegmentCoord);
                if (!currentCell.occupied)
                {                    
                    obj = Instantiate(currentWallData.cornerPreviewPrefab, World.ToTransformSpace(currentSegmentCoord), Quaternion.identity);
                    HardSnapToGrid(obj.transform, 1, 1, true);
                    wallPreviewCornerSegments.Add(obj);
                }
                // currentCell.occupied = true
                else if (IsCellOccupiedByExistingWall(currentCell) || IsCellOccupiedByConstructionWall(currentCell))
                {
                    obj = Instantiate(currentWallData.cornerPreviewPrefab, World.ToTransformSpace(currentSegmentCoord), Quaternion.identity);
                    wallPreviewCornerSegments.Add(obj);                    
                }

                // Preview objects don't have buildingDimensions on the object
                // so we have to snap ourselves.
                if (obj)
                    HardSnapToGrid(obj.transform, 1, 1, true);
            }

            //-----------------------------------------------------------------
            // North - South (Y axis)

            // Reset to start position
            currentSegmentCoord = startCoord;            
            currentSegmentCoord.x = endCoord.x;            

            for (int i = 0; i < wallSizeY - 1; ++i)
            {
                currentSegmentCoord.y += 1 * directionY;

                Cell currentCell = World.at(currentSegmentCoord);

                // Cell empty
                if (!currentCell.occupied)
                {
                    obj = Instantiate(currentWallData.worldPreviewPrefab, World.ToTransformSpace(currentSegmentCoord), Quaternion.AngleAxis(0, Vector3.up));
                    wallPreviewStraightSegments.Add(obj);
                }
                else if (IsCellOccupiedByExistingWall(currentCell) || IsCellOccupiedByConstructionWall(currentCell))
                {
                    obj = Instantiate(currentWallData.cornerPreviewPrefab, World.ToTransformSpace(currentSegmentCoord), Quaternion.identity);
                    wallPreviewCornerSegments.Add(obj);
                }

                // Preview objects don't have buildingDimensions on the object
                // so we have to snap ourselves.
                if (obj)
                    HardSnapToGrid(obj.transform, 1, 1, true);
            }
        }

        if (buildingPlacementPreviewObject.activeSelf && World.at(endCoord).occupied)
            buildingPlacementPreviewObject.SetActive(false);
        else if (!buildingPlacementPreviewObject.activeSelf && !World.at(endCoord).occupied)
            buildingPlacementPreviewObject.SetActive(true);
    }

    private bool IsCellOccupiedByExistingWall(Cell cell, bool clearCell = false)
    {
        Structure structure = cell.GetFirstOccupant<Structure>();
        if (structure?.buildingData is WallData)
        {
            if (clearCell)
            {
                structure.UnbakeFromGrid();
                structure.gameObject.SetActive(false);
                Destroy(structure.gameObject);
            }
            return true;
        }
        return false;
    }

    private bool IsCellOccupiedByConstructionWall(Cell cell, bool clearCell = false)
    {
        Constructible constructible = cell.GetFirstOccupant<Constructible>();
        if (constructible?.buildingData is WallData)
        {
            if (clearCell)
            {
                constructible.UnbakeFromGrid();
                constructible.gameObject.SetActive(false);
                Destroy(constructible.gameObject);
            }
            return true;
        }
        return false;
    }

    private void EndUnitSelectionMode()
    {
        isInUnitSelectionMode = false;
        pointedAtResource = null;
        selectedActors.Clear();
        foreach (LineRenderer lineRenderer in unitSelectionLineRenderers)
        {
            lineRenderer.enabled = false;
        }
    }

    //=========================================================================
    private void UpdatePointer()
    {
        Vector3 pointerStart = pointerStartTransform.position;
        Vector3 pointerEnd;
        Vector3 pointerDir = pointerStartTransform.forward;
        bool hitSomething = false;
        bool hitPointValid = false;
        Vector3 playerFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
        Vector3 arcVelocity = pointerDir * arcDistance;
        PointerInteractable hitPointerInteractable = null;

        // Check pointer angle
        // float dotUp = Vector3.Dot(pointerDir, Vector3.up);
        // float dotForward = Vector3.Dot(pointerDir, player.hmdTransform.forward);
        // bool pointerAtBadAngle = false;

        // if ((dotForward > 0 && dotUp > 0.75f) || (dotForward < 0.0f && dotUp > 0.5f))
        //     pointerAtBadAngle = true;

        // Trace to see if the pointer hit anything
        RaycastHit hitInfo;
        teleportArc.SetArcData(pointerStart, arcVelocity, true, false);// pointerAtBadAngle);

        teleportArc.FindProjectileCollision(out hitInfo);
        if (showPointerArc)
            teleportArc.DrawArc(out hitInfo);

        if (hitInfo.collider)
        {
            hitSomething = true;
            hitPointValid = LayerMatchTest(allowedPlacementLayers, hitInfo.collider.gameObject);

            if (selectedActors.Count > 0)
                pointedAtResource = hitInfo.collider.GetComponentInParent<Resource>();

            hitPointerInteractable = hitInfo.collider.GetComponent<PointerInteractable>();
            if (!hitPointerInteractable)
                hitPointerInteractable = hitInfo.collider.GetComponentInParent<PointerInteractable>();
        }

        HighlightSelected(hitPointerInteractable);

        if (hitPointerInteractable != null)
            pointedAtPointerInteractable = hitPointerInteractable;
        else
            pointedAtPointerInteractable = null;

        pointedAtPosition = hitInfo.point;
        pointerEnd = hitInfo.point;

        if (hitSomething)
            pointerEnd = hitInfo.point;
        else
            pointerEnd = teleportArc.GetArcPositionAtTime(teleportArc.arcDuration);

        destinationReticleTransform.position = pointedAtPosition;
        destinationReticleTransform.gameObject.SetActive(true);

        if (isSettingRallyPoint)
        {
            DrawQuadraticBezierCurve(pointerLineRenderer, rallyWaypointArcStartPosition, pointedAtPosition);
            if (pointerLineRenderer.enabled == false)
                pointerLineRenderer.enabled = true;

        }
        else if (isInUnitSelectionMode && pointedAtPointerInteractable != null)
        {                
            // Only add units to selection if trigger is pressed in more than triggerAddToSelectionThreshold
            if (selectedActors.Count <= maxUnitSelectionCount && 
                pointerHand != null &&
                uiInteractAction.GetAxis(pointerHand.handType) > triggerAddToSelectionThreshold)
            {
                ActorV2 hoveredActor = pointedAtPointerInteractable.GetComponent<ActorV2>();
                if (hoveredActor &&
                    !selectedActors.Contains(hoveredActor) && 
                    hoveredActor.Faction.IsSameFaction(faction))
                {
                    selectedActors.Add(hoveredActor);
                    PlayPointerHaptic(true);
                }
            }
        }
        else if (isInBuildingPlacementMode)
        {
            bool cellsOccupied = CellsOccupied(buildingPlacementPreviewObject.transform.position, placementBuildingData.boundingDimensionX, placementBuildingData.boundingDimensionY);

            // Gate/Wall
            if (cellsOccupied && placementBuildingData.constructionPrefab.GetComponent<Constructible>().ClearExistingWalls == true)
            {                

            }
            else if (cellsOccupied)            
            {
                MeshRenderer meshRenderer = buildingPlacementPreviewObject.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer)
                    meshRenderer.sharedMaterial = buildingPlacementInvalidMat;
                else
                    foreach (SkinnedMeshRenderer skinnedMeshRenderer in buildingPlacementPreviewObject.GetComponents<SkinnedMeshRenderer>())
                        skinnedMeshRenderer.sharedMaterial = buildingPlacementInvalidMat;
            }
            else
            {
                MeshRenderer meshRenderer = buildingPlacementPreviewObject.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer)
                    meshRenderer.sharedMaterial = buildingPlacementCachedMat;
                else
                    foreach (SkinnedMeshRenderer skinnedMeshRenderer in buildingPlacementPreviewObject.GetComponents<SkinnedMeshRenderer>())
                        skinnedMeshRenderer.sharedMaterial = buildingPlacementCachedMat;
            }

            DrawQuadraticBezierCurve(pointerLineRenderer, pointerStart, destinationReticleTransform.position);
            if (pointerLineRenderer.enabled == false)
                pointerLineRenderer.enabled = true;

            HardSnapToGrid(destinationReticleTransform, placementBuildingData.boundingDimensionX, placementBuildingData.boundingDimensionY, true);
        }
        else if (isInWallPlacementMode)
        {
            HardSnapToGrid(destinationReticleTransform, placementBuildingData.boundingDimensionX, placementBuildingData.boundingDimensionY, true);
            if (wallPlacementPreviewAnchor)// && buildingPlacementPreviewObject)
            {
                DrawWallPreviewSegments();
            }

            //DrawQuadraticBezierCurve(pointerLineRenderer, pointerStart, destinationReticleTransform.position);
            if (pointerLineRenderer.enabled == false)
                pointerLineRenderer.enabled = true;
        }
        else
            if (pointerLineRenderer.enabled == true)
            pointerLineRenderer.enabled = false;

        if (selectedActors.Count > 0)
        {
            int i = 0;
            foreach (ActorV2 actor in selectedActors)
            {
                LineRenderer lineRenderer = unitSelectionLineRenderers[i];

                if (!actor)
                {
                    selectedActors.Remove(actor);
                    if (unitSelectionLineRenderers[i].enabled)
                        unitSelectionLineRenderers[i].enabled = false;
                }
                else
                {
                    DrawQuadraticBezierCurve(unitSelectionLineRenderers[i], actor.transform.position, pointedAtPosition);
                    if (!unitSelectionLineRenderers[i].enabled)
                        unitSelectionLineRenderers[i].enabled = true;
                }
                i++;
            }
        }
    }

    //=========================================================================
    // Walls
    //-------------------------------------------------------------------------

    /// <summary>
    /// Triggered when a corner wall piece is selected from the build menu.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnBuildingPlacementStarted(object sender, BuildMenuSlot.BuildingPlacementEvent e)
    {
        if (isInBuildingPlacementMode)
            EndBuildingPlacementMode();

        if (isInWallPlacementMode)
            EndWallPlacementMode();

        // TODO: Do we want to eventually switch steam action maps based on activity?
        // SteamVR_Actions.construction.Activate();

        SetSnapTurnEnabled(false, false);
        placementBuildingData = e.buildingData;

        if (placementBuildingData is WallData)
        {
            isInWallPlacementMode = true;
            currentWallData = (WallData)placementBuildingData;

            // Instantiate wall corner as preview object and assign to reticle
            buildingPlacementPreviewObject = Instantiate(currentWallData.cornerPreviewPrefab, destinationReticleTransform);
        }
        else if (placementBuildingData is BuildingData)
        {
            isInBuildingPlacementMode = true;
            buildingPlacementPreviewObject = Instantiate(placementBuildingData.worldPreviewPrefab, destinationReticleTransform);
            buildingPlacementPreviewObject.transform.rotation = Quaternion.identity;
            
            MeshRenderer meshRenderer = buildingPlacementPreviewObject.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer)
                buildingPlacementCachedMat = meshRenderer.sharedMaterial;
            else
            {
                SkinnedMeshRenderer skinnedMeshRenderer = buildingPlacementPreviewObject.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer)
                    buildingPlacementCachedMat = skinnedMeshRenderer.sharedMaterial;
            }
        }
        else
            return;

        buildingPlacementPreviewObject.transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Cleans up and ends wall placement mode.
    /// </summary>
    private void EndWallPlacementMode()
    {
        isInWallPlacementMode = false;

        ClearWallPreviewSections();

        // TODO: Destroy these after the start/end pieces have been instantiated.
        if (wallPlacementPreviewAnchor)
            Destroy(wallPlacementPreviewAnchor);

        if (buildingPlacementPreviewObject)
            Destroy(buildingPlacementPreviewObject);

        wallPlacementPreviewAnchor = null;
        buildingPlacementPreviewObject = null;

        SetSnapTurnEnabled(true, true);
    }

    private bool CanInteract(Hand hand)
    {
        // !PlayerManager.instance.handBuildMenu.activeSelf
        if (!hand.currentAttachedObject && !hand.hoveringInteractable)
            return true;

        return false;
    }

    private bool WasTeleportButtonReleased(Hand hand)
    {
        // if (IsEligibleForTeleport(hand))
        return teleportAction.GetStateUp(hand.handType);

        //return false;
    }

    public bool IsEligibleForTeleport(Hand hand)
    {
        // if (player.rigSteamVR.transform.localScale.x > 2.0f)
        //     return false;

        return true;

        // TODO: Clean this up so it works for both hands. Ideally, just have different action
        // sets.
        // if (isInBuildingPlacementMode && hand.handType == SteamVR_Input_Sources.RightHand)
        //     return false;

        // if (hand == null)
        //     return false;

        // if (!hand.gameObject.activeInHierarchy)
        //     return false;

        // if (hand.hoveringInteractable != null)
        //     return false;

        // if (hand.noSteamVRFallbackCamera == null)
        // {
        //     if (hand.isActive == false)
        //         return false;

        //     // Something is attached to the hand
        //     if (hand.currentAttachedObject != null)
        //     {
        //         AllowTeleportWhileAttachedToHand allowTeleportWhileAttachedToHand = hand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();

        //         if (allowTeleportWhileAttachedToHand != null && allowTeleportWhileAttachedToHand.teleportAllowed == true)
        //             return true;
        //         else
        //             return false;
        //     }
        // }

        //return true;
    }

    private bool WasTeleportButtonPressed(Hand hand)
    {
        if (IsEligibleForTeleport(hand))
            return teleportAction.GetStateDown(hand.handType);
        //return hand.controller.GetPressDown( SteamVR_Controller.ButtonMask.Touchpad );

        return false;
    }

    private void TryTeleportPlayer()
    {
        if (!teleporting)
        {
            // TODO: Change this code to use buildings as teleport markers and when
            // teleporting to buildings the menu for them is possibly displayed if
            // it has a build/upgrade options.

            InitiateTeleportFade();

            // if (pointedAtTeleportMarker != null && pointedAtTeleportMarker.locked == false)
            // {
            //     // Pointing at an unlocked teleport marker
            //     teleportingToMarker = pointedAtTeleportMarker;

            //     InitiateTeleportFade();
            //     CancelTeleportHint();
            // }
        }
    }

    private void InitiateTeleportFade()
    {
        teleporting = true;
        currentFadeTime = teleportFadeTime;

        // TeleportPoint teleportPoint = teleportingToMarker as TeleportPoint;
        // if ( teleportPoint != null && teleportPoint.teleportType == TeleportPoint.TeleportPointType.SwitchToNewScene )
        // {
        // 	currentFadeTime *= 3.0f;
        // 	Teleport.ChangeScene.Send( currentFadeTime );
        // }

        SteamVR_Fade.Start(Color.clear, 0);
        SteamVR_Fade.Start(Color.black, currentFadeTime);
        PlayerManager.Instance.PlayTeleportSound();

        Invoke("TeleportPlayer", currentFadeTime);
    }

    private void TeleportPlayer()
    {
        teleporting = false;
        SteamVR_Fade.Start(Color.clear, currentFadeTime);
        Vector3 teleportPosition = pointedAtPosition;

        // if ( teleportingToMarker.ShouldMovePlayer() )
        // {
        Vector3 playerFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
        player.trackingOriginTransform.position = teleportPosition + playerFeetOffset;

        if (player.leftHand.currentAttachedObjectInfo.HasValue)
            player.leftHand.ResetAttachedTransform(player.leftHand.currentAttachedObjectInfo.Value);
        if (player.rightHand.currentAttachedObjectInfo.HasValue)
            player.rightHand.ResetAttachedTransform(player.rightHand.currentAttachedObjectInfo.Value);
        // }
        // else
        // {
        // 	teleportingToMarker.TeleportPlayer( pointedAtPosition );
        // }

        showPointerArc = false;
    }

    public void DrawQuadraticBezierCurve(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        float dist = Vector3.Distance(end, start) * 0.5f;
        Vector3 dir = (end - start).normalized;
        Vector3 mid = start + (dir * dist);
        mid.y += 1;

        lineRenderer.positionCount = 200;
        float t = 0f;
        Vector3 B = new Vector3(0, 0, 0);
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            B = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * mid + t * t * end;
            lineRenderer.SetPosition(i, B);
            t += (1 / (float)lineRenderer.positionCount);
        }
    }

    private static bool LayerMatchTest(LayerMask layerMask, GameObject obj)
    {
        return ((1 << obj.layer) & layerMask) != 0;
    }

    private void HookIntoEvents()
    {
        BuildMenuSlot.OnBuildingPlacementEvent += OnBuildingPlacementStarted;
    }

    private void CleanupEvents()
    {
        BuildMenuSlot.OnBuildingPlacementEvent -= OnBuildingPlacementStarted;
    }
    private void HidePointer()
    {
        if (showPointerArc)
            pointerHideStartTime = Time.time;

        showPointerArc = false;
        //pointerObject.SetActive( false );
        teleportArc.Hide();
    }


    //=========================================================================
    private void ShowPointer()
    {
        if (!showPointerArc)
        {
            pointedAtPointerInteractable = null;
            pointerShowStartTime = Time.time;
            showPointerArc = true;
            //pointerObject.SetActive( false );
            teleportArc.Show();

            // foreach ( PointerInteractable interactObject in interactableObjects )
            // 	interactObject.Highlight( false );

            //startingFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
            //movedFeetFarEnough = false;
        }

        pointerStartTransform = pointerAttachmentPoint.transform;

        // if (pointerHand.currentAttachedObject != null)
        // {
        //     //allowTeleportWhileAttached = pointerHand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();
        // }
    }

    private bool WasQueueButtonPressed(Hand hand)
    {
        return queueAction.GetStateDown(hand.handType);
    }

    private bool WasSelectButtonReleased(Hand hand)
    {
        return selectAction.GetStateUp(hand.handType);
    }

    private bool WasQueueButtonReleased(Hand hand)
    {
        return queueAction.GetStateUp(hand.handType);
    }

    private bool WasDequeueButtonPressed(Hand hand)
    {
        return dequeueAction.GetStateDown(hand.handType);
    }

    private bool WasDequeueButtonReleased(Hand hand)
    {
        return dequeueAction.GetStateUp(hand.handType);
    }

    private bool WasRotateClockwiseButtonPressed(Hand hand)
    {
        return rotateBuildingClockwiseAction.GetStateDown(hand.handType);
    }

    private bool WasRotateCounterclockwiseButtonPressed(Hand hand)
    {
        return rotateBuildingCounterclockwiseAction.GetStateDown(hand.handType);
    }

    private bool WasSelectButtonPressed(Hand hand)
    {
        return selectAction.GetStateDown(hand.handType);
    }

    private bool WasCancelButtonPressed(Hand hand)
    {
        return cancelAction.GetStateDown(hand.handType);
    }

    private bool WasCancelButtonReleased(Hand hand)
    {
        return cancelAction.GetStateUp(hand.handType);
    }

    private void SetSnapTurnEnabled(bool right, bool left)
    {
        Player.instance.GetComponentInChildren<SnapTurn>().rightHandEnabled = right;
        Player.instance.GetComponentInChildren<SnapTurn>().leftHandEnabled = left;
        Player.instance.GetComponentInChildren<SnapTurn>().enabled = right && left;
    }

    public void HardSnapToGrid(Transform obj, int boundingDimensionX, int boundingDimensionY, bool verticalSnap = false)
    {
        Vector3 pos = World.ToWorldSpace(obj.position);        

        obj.position = World.ToTransformSpace(new Vector3(Mathf.RoundToInt(pos.x), pos.y, Mathf.RoundToInt(pos.z)));

        Vector3 modPos = obj.position;
        if (boundingDimensionX % 2 == 0)
            modPos.x = obj.position.x + World.GetUnit() * -0.5f;

        if (boundingDimensionY % 2 == 0)
            modPos.z = obj.position.z + World.GetUnit() * -0.5f;

        // TODO: Vertical snapping should snap to terrain dynamically
        float positionY = verticalSnap == true ? 0.0f : obj.position.y;

        if (verticalSnap)
        {
            RaycastHit hit;
            Vector3 sourceLocation = obj.position;
            sourceLocation.y += 10.0f;

            if (Physics.Raycast(sourceLocation, Vector3.down, out hit, 30.0f, allowedPlacementLayers))
                modPos.y = hit.point.y;
        }

        obj.position = modPos;
    }


    //=========================================================================
    private void PlayPointerHaptic(bool validLocation)
    {
        if (pointerHand != null)
            if (validLocation)
                pointerHand.TriggerHapticPulse(800);
            else
                pointerHand.TriggerHapticPulse(100);
    }


    private void HighlightSelected(PointerInteractable hitPointerInteractable)
    {
        // Pointing at a new interactable
        if (pointedAtPointerInteractable != hitPointerInteractable)
        {
            if (pointedAtPointerInteractable != null)
                pointedAtPointerInteractable.Highlight(false);

            if (hitPointerInteractable != null)
            {
                hitPointerInteractable.Highlight(true);
                prevPointedAtPosition = pointedAtPosition;
                PlayPointerHaptic(true);//!hitPointerInteractable.locked );
                                        // PlayAudioClip( reticleAudioSource, goodHighlightSound );
                                        // loopingAudioSource.volume = loopingAudioMaxVolume;
            }
            else if (pointedAtPointerInteractable != null)
            {
                // PlayAudioClip( reticleAudioSource, badHighlightSound );
                // loopingAudioSource.volume = 0.0f;
            }
        }
        // Pointing at the same interactable
        else if (hitPointerInteractable != null)
        {
            if (Vector3.Distance(prevPointedAtPosition, pointedAtPosition) > 1.0f)
            {
                prevPointedAtPosition = pointedAtPosition;
                PlayPointerHaptic(true); //!hitPointerInteractable.locked );
            }
        }
    }

    //=========================================================================
    private bool ShouldOverrideHoverLock()
    {
        if (!allowTeleportWhileAttached || allowTeleportWhileAttached.overrideHoverLock)
            return true;

        return false;
    }

    //=========================================================================
    void OnDisable()
    {
        HidePointer();
    }

    void OnDestroy()
    {
        CleanupEvents();
    }
}

