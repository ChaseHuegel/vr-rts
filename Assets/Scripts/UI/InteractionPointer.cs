
using System;
using System.Collections.Generic;
using System.Linq;
using Swordfish;
using Swordfish.Audio;
using Swordfish.Navigation;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using System.Collections;
using UnityEngine.Events;

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
    public SteamVR_Action_Single squeezeAction = SteamVR_Input.GetAction<SteamVR_Action_Single>("Squeeze");
    public SteamVR_Action_Boolean grabGripAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
    public SteamVR_Action_Boolean showMenuAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("ToggleMainMenu");
  
    //=========================================================================
    [Header("Layer Masks")]
    public LayerMask traceLayerMask;
    public LayerMask buildingPlacementMask;
    public LayerMask unitSelectionMask;

    //=========================================================================
    [Header("Pointer")]
    public Transform pointerReticle;
    public Color pointerValidColor;
    public Color pointerInvalidColor;
    public float arcDistance = 10.0f;   

    //=========================================================================
    [Header("Teleport")]
    public float teleportFadeTime = 0.1f;

    //=========================================================================
    [Header("Misc")]
    public Hand handReticle;
    public bool useHandAsReticle;
    public GameObject wayPointReticle;
    public GameObject setRallyPointPrefab;

    public QueueUnitButton PointedAtQueueButton 
    { 
        get 
        {
            QueueUnitButton queueUnitButton = currentInteractable.GetComponentInChildren<QueueUnitButton>();
            if (queueUnitButton == null)
                queueUnitButton = currentInteractable.GetComponentInParent<QueueUnitButton>();

            return queueUnitButton;
        }
    }

    private SpawnQueue spawnQueue;
    private Hand pointerHand = null;
    private List<ActorV2> selectedActors;
    private Vector3 pointedAtPosition;
    private Vector3 prevPointedAtPosition;
    private float pointerShowStartTime = 0.0f;
    private float pointerHideStartTime = 0.0f;
    private AllowTeleportWhileAttachedToHand allowTeleportWhileAttached = null;
    
    private bool teleporting = false;
    private float currentFadeTime = 0.0f;
    
    private Resource pointedAtResource;
    private Vector3 rallyWaypointArcStartPosition;    
    private float triggerAddToSelectionThreshold = 0.85f;
    private GripPan gripPan;
    private GameObject hintObject;
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
    public Material buildingPlacementInvalidMat;    
    public float buildingPlacementRotationIncrement = 90.0f;
    public float wallPlacementRotationIncrement = 45f;

    private GameObject bldngPreview;
    private float lastBuildingRotation;
    private BuildingData curBldngData;
    private Material buildingPlacementCachedMat;

    //=========================================================================
    // Wall Related
    private GameObject wallPlacementPreviewAnchor;

    //=========================================================================
    // Cached wall objects
    private WallData currentWallData;
    private List<GameObject> wallPreviewDiagonalSegments = new List<GameObject>();
    private List<GameObject> wallPreviewCornerSegments = new List<GameObject>();
    private List<GameObject> wallPreviewStraightSegments = new List<GameObject>();

    //=========================================================================
    // Vertical placement mode
    private bool isVerticalPlacementModeActive = true; 
    public GameObject verticalLaserPrefab;
    private GameObject vertLaserObject;
    
    private TeleportArc teleportArc = null;
    private bool showPointerArc = false;
    private Interactable currentInteractable;
    private LineRenderer pointerLineRenderer;
    private LineRenderer[] unitSelectionLineRenderers;
    private Transform pointerStartTransform;

    //=========================================================================
    private static InteractionPointer _instance;
    public static InteractionPointer Instance
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindFirstObjectByType<InteractionPointer>();

            return _instance;
        }
    }

    //=========================================================================
    void Awake()
    {
        _instance = this;

        pointerLineRenderer = GetComponentInChildren<LineRenderer>();
        handReticle.enabled = useHandAsReticle;

#if UNITY_URP
		fullTintAlpha = 0.5f;
#endif
        teleportArc = GetComponent<TeleportArc>();
        teleportArc.traceLayerMask = traceLayerMask;        
    }

    //=========================================================================
    void Start()
    {
        HookIntoEvents();

        playerManager = PlayerManager.Instance;
        //interactableObjects = GameObject.FindObjectsOfType<Interactable>();
        selectedActors = new List<ActorV2>();
        gripPan = Player.instance.GetComponent<GripPan>();

        // Cache some values
        maxUnitSelectionCount = GameMaster.Instance.maximumUnitSelectionCount;
        faction = playerManager.faction;
        
        InitializeUnitSelectionLineRenderers();

        if (Player.instance == null)
        {
            Debug.LogError("<b>[SteamVR Interaction]</b> InteractionPointer: No Player instance found in map.", this);
            Destroy(this.gameObject);
            return;
        }

        vertLaserObject = Instantiate(verticalLaserPrefab, Vector3.zero, Quaternion.identity);
        HideVerticalLaser();

        // Initialize reticle
        //ShowPointer();
        pointerStartTransform = Player.instance.rightHand.panTransform;// pointerAttachmentPoint.transform;
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

    public void DisableInteraction() => this.enabled = false;
    public void EnableInteraction() => this.enabled = true;

    private void TryShowVerticalPlacementLaser()
    {
        RaycastHit hit;

        // Ray from anchor
        if (Physics.Raycast(Player.instance.rightHand.panTransform.position, Vector3.down, out hit, 100, buildingPlacementMask))
        {
            PointLaser(hit);
        }
        else
            HideVerticalLaser();
    }

    private void PointLaser(RaycastHit hit)
    {
        ShowVerticalLaser();

        // Position laser between controller and point where raycast hits. Use Lerp because you can
        // give it two positions and the % it should travel. If you pass it .5f, which is 50%
        // you get the precise middle point.
        vertLaserObject.transform.position = Vector3.Lerp(Player.instance.rightHand.panTransform.position, hit.point, .5f);

        // Point the laser at position where raycast hits.
        vertLaserObject.transform.LookAt(hit.point);

        // Scale the laser so it fits perfectly between the two positions
        vertLaserObject.transform.localScale = new Vector3(vertLaserObject.transform.localScale.x,
            vertLaserObject.transform.localScale.y, hit.distance);

        // Reticle
        if (bldngPreview)
        {
            bldngPreview.transform.position = hit.point;// + vrReticleOffset;
            HardSnapToGrid(bldngPreview.transform, curBldngData.boundingDimensionX, curBldngData.boundingDimensionY, true);
        }

    }

    private void ShowVerticalLaser() => vertLaserObject.SetActive(true);
    private void HideVerticalLaser() => vertLaserObject.SetActive(false);

    //=========================================================================
    void Update()
    {
        UpdatePointer();

        foreach (Hand hand in Player.instance.hands)
        {
            // Breaks unit selection mode when build menu is active
            // if (isInUnitSelectionMode == true && hand.currentAttachedObject != null &&
            //     hand.currentAttachedObject != PlayerManager.Instance.Buildmenu)
            // {
            //     EndUnitSelectionMode();
            //     return;
            // }

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
                Process_InteractUI_Action_End();

            else if (WasInteractButtonPressed(hand))
                Process_InteractUI_Action_Start(hand);

            else if (WasShowMenuButtonPressed(hand))
            {
                PlayerManager.Instance.ToggleMainMenu();
            }

            else if (WasCancelButtonPressed(hand))
            {
                pointerHand = hand;
                if (isInUnitSelectionMode)
                    EndUnitSelectionMode();
                else if (isInBuildingPlacementMode)
                    EndBuildingPlacementMode();
                else if (currentInteractable)
                {
                    WallGate wallGate = currentInteractable.GetComponent<WallGate>();
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
                if (currentInteractable)
                {
                    if (TryToggleBuildingInteractionPanel(currentInteractable.gameObject))
                        return;

                    //-----------------------------------------------------------------
                    // Queue/Cancel buttons in building interaction panels  
                    if (TryInvokeHoverButton(hand, currentInteractable.gameObject))
                        return;    

                    if (TryToggleWallGate(currentInteractable.gameObject))
                        return;
                }          
            }            
            else if (WasSelectButtonReleased(hand))
            {
                if (pointerHand == hand)
                    pointerHand = null;
            }
            else if (WasRotateClockwiseButtonPressed(hand))
            {
                if (isInBuildingPlacementMode)
                {
                    float rotationIncrement = buildingPlacementRotationIncrement;
                    if (curBldngData.buildingType == BuildingType.WallGate)
                        rotationIncrement = wallPlacementRotationIncrement;

                    bldngPreview.transform.Rotate(0.0f, -rotationIncrement, 0.0f);
                }
                else
                {                    
                    PlayerManager.Instance.ProcessRotateClockwiseEvent(hand);
                }
            }
            // else if (WasRotateClockwiseButtonReleased(hand)) { }
            // else if (WasRotateCounterclockwiseButtonReleased(hand)) { }
            
            else if (WasRotateCounterclockwiseButtonPressed(hand))
            {
                if (isInBuildingPlacementMode)
                {
                    float rotationIncrement = buildingPlacementRotationIncrement;
                    if (curBldngData.buildingType == BuildingType.WallGate)
                        rotationIncrement = wallPlacementRotationIncrement;

                    bldngPreview.transform.Rotate(0.0f, rotationIncrement, 0.0f);
                }
                else
                {
                    PlayerManager.Instance.ProcessRotateCounterClockwiseEvent(hand);
                }
            }
                      
            /* if (WasQueueButtonPressed(hand))
                newPointerHand = hand;

            if (WasQueueButtonReleased(hand) && pointedAtInteractable)
            {
                if (pointerHand == hand)
                {
                    SpawnQueue buildingSpawnQueue = pointedAtInteractable.GetComponentInChildren<SpawnQueue>();
                    if (buildingSpawnQueue && buildingSpawnQueue.QueueLastUnitQueued())
                        PlayAudioClip(headAudioSource, queueSuccessSound);
                    else
                        PlayAudioClip(headAudioSource, queueFailedSound);
                }
            }

            if (WasDequeueButtonPressed(hand))
                newPointerHand = hand;

            if (WasDequeueButtonReleased(hand) && pointedAtInteractable)
            {
                if (pointerHand == hand)
                {
                    SpawnQueue buildingSpawnQueue = pointedAtInteractable.GetComponentInChildren<SpawnQueue>();
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
    private void Process_InteractUI_Action_Start(Hand hand)
    {
        if (isInWallPlacementMode)
        {
            if (!wallPlacementPreviewAnchor)
            {
                wallPlacementPreviewAnchor = Instantiate(bldngPreview, bldngPreview.transform.position, Quaternion.identity);
                //wallPlacementPreviewAnchor.transform.Rotate(0, lastBuildingRotation, 0);
            }
            
            currentWallData = (WallData)curBldngData;
        }
        else if (currentInteractable != null)
        {
            spawnQueue = currentInteractable.GetComponentInChildren<SpawnQueue>();

            if (spawnQueue && spawnQueue.enabled && !isSettingRallyPoint)
            {
                rallyWaypointArcStartPosition = currentInteractable.transform.position;
                isSettingRallyPoint = true;
                wayPointReticle.SetActive(true);
                return;
            }

            ActorV2 hoveredActor = currentInteractable.GetComponent<ActorV2>();
            if (hoveredActor &&
                !isInUnitSelectionMode &&
                hoveredActor.Faction.IsSameFaction(faction))
            {
                selectedActors.Add(hoveredActor);
                isInUnitSelectionMode = true;
                return;
            }

            //-----------------------------------------------------------------
            // Queue/Cancel buttons in building interaction panels  
            if (TryInvokeHoverButton(hand, currentInteractable.gameObject))
                return;

            if (TryToggleWallGate(currentInteractable.gameObject))
                return;
            
        }
        else if (isInBuildingPlacementMode)
        {
            return;
        }
        // Start unit selection mode if no interactible object is pointed at.
        else if (currentInteractable == null)
        {
            isInUnitSelectionMode = true;
        }
    }

    private bool TryToggleWallGate(GameObject gameObject)
    {
        WallGate wallGate = gameObject.GetComponent<WallGate>();
        if (wallGate)
        {
            wallGate.ToggleDoors();
            return true;
        }

        return false;
    }

    private bool TryInvokeHoverButton(Hand hand, GameObject gameObject)
    {
        // Hands use SendMessage to call events on targeted 'Interactable' so for our
        // 'Interactable' we use GetComponent instead. Should probably switch
        // hands to use GetComponent as well for performance.
        HoverButton hoverButton = gameObject.GetComponentInChildren<HoverButton>();
        if (hoverButton)
        {
            hoverButton.onButtonDown.Invoke(hand);
            return true;
        }

        return false;
    }

    private bool TryToggleBuildingInteractionPanel(GameObject gameObject)
    {
        BuildingInteractionPanel buildingInteractionPanel = gameObject.GetComponentInChildren<BuildingInteractionPanel>();
        if (buildingInteractionPanel)
        {
            buildingInteractionPanel.Toggle();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Executes an interaction. The interation has been accepted and this function
    /// completes it, in contrast to a cancelled interaction.
    /// </summary>
    private void Process_InteractUI_Action_End()
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
            bool cellsOccupied = World.CellsOccupied(bldngPreview.transform.position, curBldngData.boundingDimensionX, curBldngData.boundingDimensionY, curBldngData.allowedLayers);
            
            if (cellsOccupied)
            {
                if (curBldngData.constructionPrefab.GetComponent<Constructible>().ClearExistingWalls == true)
                {
                    Cell currentCell = World.at(World.ToWorldCoord(bldngPreview.transform.position));
                    if (currentCell.GetFirstOccupant<Body>().GetComponent<WallSegment>())
                    {
                        PlayerManager.Instance.PlayBuildingPlacedSound();
                        GameObject gameObject = Instantiate(curBldngData.constructionPrefab, bldngPreview.transform.position, bldngPreview.transform.rotation);
                        gameObject.GetComponent<Constructible>().Faction = this.faction;
                        PlayerManager.Instance.DeductTechResourceCost(curBldngData);
                    }
                }
                else
                    PlayerManager.Instance.PlayBuildingPlacementDeniedAudio();
            }
            else if (!cellsOccupied)
            {
                PlayerManager.Instance.PlayBuildingPlacedSound();
                GameObject gameObject = Instantiate(curBldngData.constructionPrefab, bldngPreview.transform.position, bldngPreview.transform.rotation);
                gameObject.GetComponent<Constructible>().Faction = this.faction;
                PlayerManager.Instance.DeductTechResourceCost(curBldngData);            
            }

            EndBuildingPlacementMode();                                  
        }

        else if (isSettingRallyPoint)
        {
            // TODO: Draw line to rally point.
            spawnQueue.SetUnitRallyPointPosition(wayPointReticle.transform.position);
            wayPointReticle.SetActive(false);

            GameObject gameObject = Instantiate<GameObject>(setRallyPointPrefab, wayPointReticle.transform.position, wayPointReticle.transform.rotation);
            gameObject.transform.localScale = setRallyPointPrefab.transform.localScale;
            gameObject.GetComponentInChildren<Animator>().Play("deploy");
            Destroy(gameObject, 2.0f);

            PlayerManager.Instance.PlaySetRallyPointSound();
            spawnQueue = null;
            isSettingRallyPoint = false;
            DisablePointerArcRendering();

            return;
        }
        else if (selectedActors.Count > 0)
        {
            foreach (ActorV2 actor in selectedActors)
            {
                Body body = currentInteractable?.GetComponents<Body>().FirstOrDefault(x => x.enabled);
                if (currentInteractable && body)
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
        Instantiate(currentWallData.cornerConstructionPrefab, bldngPreview.transform.position, gameObject.transform.rotation);


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

    private void EndBuildingPlacementMode()
    {
        isInBuildingPlacementMode = false;
        Destroy(bldngPreview);
        bldngPreview = null;
        buildingPlacementCachedMat = null;

        //lastBuildingRotation = buildingPlacementPreviewObject.transform.localRotation.eulerAngles.z;  

        HideVerticalLaser();
        // TODO: Restore resources to player?

        // Reenable snap turn since it's turned off for rotating building using sticks
        // Should be unnecessary with different steam profiles for different action sets if we decide
        // to go down that road. 
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
        Vector3 reticlePosition = bldngPreview.transform.position;

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

        if (bldngPreview.activeSelf && World.at(endCoord).occupied)
            bldngPreview.SetActive(false);
        else if (!bldngPreview.activeSelf && !World.at(endCoord).occupied)
            bldngPreview.SetActive(true);
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

    private bool IsAddingToUnitSelection() 
    {
        if (!pointerHand)
            return false;
            
        return uiInteractAction.GetAxis(pointerHand.handType) > triggerAddToSelectionThreshold; 
    }

    //=========================================================================
    private void UpdatePointer()
    {        
        Vector3 pointerStart = pointerStartTransform.position;
        Vector3 pointerEnd;
        Vector3 pointerDir = pointerStartTransform.forward;
        bool hitSomething = false;
        bool hitPointValid = false;
        Vector3 playerFeetOffset = Player.instance.trackingOriginTransform.position - Player.instance.feetPositionGuess;
        Vector3 arcVelocity = pointerDir * arcDistance;
        Interactable hitInteractable = null;
        
        // Trace to see if the pointer hit anything
        RaycastHit hitInfo;
        teleportArc.SetArcData(pointerStart, arcVelocity, true, false);;

        if (isInUnitSelectionMode && IsAddingToUnitSelection())
            teleportArc.traceLayerMask = unitSelectionMask;
        else if (isInBuildingPlacementMode)
            teleportArc.traceLayerMask = buildingPlacementMask;
        else
            teleportArc.traceLayerMask = traceLayerMask;

        teleportArc.FindProjectileCollision(out hitInfo);
        if (showPointerArc)
            teleportArc.DrawArc(out hitInfo);

        if (hitInfo.collider)
        {
            hitSomething = true;
            hitPointValid = LayerMatchTest(buildingPlacementMask, hitInfo.collider.gameObject);

            if (selectedActors.Count > 0)
                pointedAtResource = hitInfo.collider.GetComponentInParent<Resource>();

            hitInteractable = hitInfo.collider.GetComponent<Interactable>();
            if (!hitInteractable)
                hitInteractable = hitInfo.collider.GetComponentInParent<Interactable>();
        }

        TryHighlightSelected(hitInteractable);

        // teleportArc.SetColor(pointerValidColor);
        // pointerLineRenderer.startColor = pointerValidColor;
        // pointerLineRenderer.endColor = pointerValidColor;

        // teleportArc.SetColor(pointerInvalidColor);
        // pointerLineRenderer.startColor = pointerInvalidColor;
        // pointerLineRenderer.endColor = pointerInvalidColor;

        if (hitInteractable != null)
            currentInteractable = hitInteractable;
        else
            currentInteractable = null;

        ShowObjectHint(hitInteractable);

        pointedAtPosition = hitInfo.point;
        pointerEnd = hitInfo.point;

        if (hitSomething)
            pointerEnd = hitInfo.point;
        else
            pointerEnd = teleportArc.GetArcPositionAtTime(teleportArc.arcDuration);

        pointerReticle.position = pointedAtPosition;
        pointerReticle.gameObject.SetActive(true);

        // TODO: Change reticle collision layers when setting rally point?
        if (isSettingRallyPoint)
        {
            DrawQuadraticBezierCurve(pointerLineRenderer, rallyWaypointArcStartPosition, pointedAtPosition);
            EnablePointerArcRendering();

            //HardSnapToGrid(destinationReticleTransform, 1, 1, false);

        }
        else if (isInUnitSelectionMode && currentInteractable != null)
        {                
            // Only add units to selection if trigger is pressed in more than triggerAddToSelectionThreshold
            if (selectedActors.Count < maxUnitSelectionCount && 
                pointerHand != null && IsAddingToUnitSelection())
            {
                ActorV2 hoveredActor = currentInteractable.GetComponent<ActorV2>();
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
            bool cellsOccupied = World.CellsOccupied(bldngPreview.transform.position, curBldngData.boundingDimensionX, curBldngData.boundingDimensionY, curBldngData.allowedLayers);
            
            // Gate/Wall
            if (cellsOccupied && curBldngData.constructionPrefab.GetComponent<Constructible>().ClearExistingWalls == true)
            {                

            }
            else if (cellsOccupied)            
            {
                MeshRenderer meshRenderer = bldngPreview.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer)
                    meshRenderer.sharedMaterial = buildingPlacementInvalidMat;
                else
                    foreach (SkinnedMeshRenderer skinnedMeshRenderer in bldngPreview.GetComponents<SkinnedMeshRenderer>())
                        skinnedMeshRenderer.sharedMaterial = buildingPlacementInvalidMat;
            }
            else
            {
                MeshRenderer meshRenderer = bldngPreview.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer)
                    meshRenderer.sharedMaterial = buildingPlacementCachedMat;
                else
                    foreach (SkinnedMeshRenderer skinnedMeshRenderer in bldngPreview.GetComponents<SkinnedMeshRenderer>())
                        skinnedMeshRenderer.sharedMaterial = buildingPlacementCachedMat;
            }

            // TODO: Integrate this into input loop 
            if (grabGripAction.GetState(Player.instance.rightHand.handType))
            //if (squeezeAction.GetAxis(SteamVR_Input_Sources.RightHand) > 0.0f)
            // if (player.rightHand.skeleton.fingerCurls[2] >= 0.75f && 
            //     player.rightHand.skeleton.fingerCurls[3] >= 0.75f &&
            //     player.rightHand.skeleton.fingerCurls[4] >= 0.75f)
            {
                isVerticalPlacementModeActive = true;
                gripPan.DisablePanning(Player.instance.rightHand);
            }
            else
            {
                if (isVerticalPlacementModeActive)
                {
                    isVerticalPlacementModeActive = false;
                    bldngPreview.transform.localPosition = Vector3.zero;
                    HideVerticalLaser();
                    gripPan.EnablePanning(Player.instance.rightHand);
                }
            }

            if (isVerticalPlacementModeActive)
            {
                TryShowVerticalPlacementLaser();
                DisablePointerArcRendering();
            }
            else
            {
                DrawQuadraticBezierCurve(pointerLineRenderer, pointerStart, pointerReticle.position);
                EnablePointerArcRendering();
                HardSnapToGrid(pointerReticle, curBldngData.boundingDimensionX, curBldngData.boundingDimensionY, true);
            }
        }
        else if (isInWallPlacementMode)
        {
            HardSnapToGrid(pointerReticle, curBldngData.boundingDimensionX, curBldngData.boundingDimensionY, true);
            if (wallPlacementPreviewAnchor)// && buildingPlacementPreviewObject)
            {
                // ! This is to reduce DrawWallPreview calls, commented out for testing.
                // if (lastPreviewPointerPosition != World.ToWorldCoord(reticlePosition))
                // {
                //     DrawWallPreviewSegments();
                //     lastPreviewPointerPosition = World.ToWorldCoord(reticlePosition);
                // }

                DrawWallPreviewSegments();
            }

            //DrawQuadraticBezierCurve(pointerLineRenderer, pointerStart, destinationReticleTransform.position);

            EnablePointerArcRendering();
        }
        else
            DisablePointerArcRendering();
   
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

    private void DisablePointerArcRendering()
    {
        pointerLineRenderer.enabled = false;
    }

    private void EnablePointerArcRendering()
    {
        pointerLineRenderer.enabled = true;
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
        curBldngData = e.buildingData;

        if (curBldngData is WallData)
        {
            isInWallPlacementMode = true;
            currentWallData = (WallData)curBldngData;

            // Instantiate wall corner as preview object and assign to reticle
            bldngPreview = Instantiate(currentWallData.cornerPreviewPrefab, pointerReticle);
        }
        else if (curBldngData is BuildingData)
        {
            isInBuildingPlacementMode = true;
            bldngPreview = Instantiate(curBldngData.worldPreviewPrefab, pointerReticle);
            bldngPreview.transform.rotation = Quaternion.identity;
            
            MeshRenderer meshRenderer = bldngPreview.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer)
                buildingPlacementCachedMat = meshRenderer.sharedMaterial;
            else
            {
                SkinnedMeshRenderer skinnedMeshRenderer = bldngPreview.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer)
                    buildingPlacementCachedMat = skinnedMeshRenderer.sharedMaterial;
            }
        }
        else
            return;

        bldngPreview.transform.localPosition = Vector3.zero;
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

        if (bldngPreview)
            Destroy(bldngPreview);

        wallPlacementPreviewAnchor = null;
        bldngPreview = null;
        
        HideVerticalLaser();

        SetSnapTurnEnabled(true, true);
    }

    private bool CanInteract(Hand hand)
    {
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
        Vector3 playerFeetOffset = Player.instance.trackingOriginTransform.position - Player.instance.feetPositionGuess;
        Player.instance.trackingOriginTransform.position = teleportPosition + playerFeetOffset;

        if (Player.instance.leftHand.currentAttachedObjectInfo.HasValue)
            Player.instance.leftHand.ResetAttachedTransform(Player.instance.leftHand.currentAttachedObjectInfo.Value);
        if (Player.instance.rightHand.currentAttachedObjectInfo.HasValue)
            Player.instance.rightHand.ResetAttachedTransform(Player.instance.rightHand.currentAttachedObjectInfo.Value);
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
            currentInteractable = null;
            pointerShowStartTime = Time.time;
            showPointerArc = true;
            //pointerObject.SetActive( false );
            teleportArc.Show();

            // foreach (Interactable interactObject in interactableObjects)
            //     interactObject.Highlight( false );

        }

        pointerStartTransform = Player.instance.rightHand.panTransform; //pointerAttachmentPoint.transform;

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

    private bool WasRotateClockwiseButtonReleased(Hand hand)
    {
        // if (pointerHand == hand)
        //     pointerHand = null;

        return rotateBuildingClockwiseAction.GetStateUp(hand.handType);
    }

    private bool WasRotateCounterclockwiseButtonReleased(Hand hand)
    {
        // if (pointerHand == hand)
        //     pointerHand = null;

        return rotateBuildingCounterclockwiseAction.GetStateUp(hand.handType);
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

    private bool WasShowMenuButtonPressed(Hand hand)
    {
        return showMenuAction.GetStateDown(hand.handType);
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

        // Vertical snapping
        float positionY = verticalSnap == true ? 0.0f : obj.position.y;

        if (verticalSnap)
        {
            RaycastHit hit;
            Vector3 sourceLocation = obj.position;
            sourceLocation.y += 10.0f;

            if (Physics.Raycast(sourceLocation, Vector3.down, out hit, 30.0f, buildingPlacementMask))
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

    private void ShowObjectHint(Interactable targetInteractable)
    {
        // Different interactable
        // if (currentInteractable != targetInteractable)
        // { 
        //     if (!hintObject)
        //         hintObject = Instantiate(GameMaster.Instance.worldButtonHintPrefab, targetInteractable.transform);

        //     hintObject.transform.SetParent(targetInteractable.transform);
        //     hintObject.transform.localPosition = new Vector3(0.0f, 0.65f, -0.2f);
        //     hintObject.transform.localRotation = Quaternion.identity;
        // }
    }

    private void HideObjectHint()
    { }

    private void TryHighlightSelected(Interactable targetInteractable)
    {
        // Pointing at a new interactable
        if (currentInteractable != targetInteractable)
        {
            if (currentInteractable != null)
                currentInteractable.HighlightOff();

            if (targetInteractable != null)
            {
                targetInteractable.TryHighlight();
                prevPointedAtPosition = pointedAtPosition;
                PlayPointerHaptic(true);
                // PlayPointerHaptic(!hitInteractable.locked );
                // PlayAudioClip( reticleAudioSource, goodHighlightSound );
                // loopingAudioSource.volume = loopingAudioMaxVolume;
            }
            // else if (currentInteractable != null)
            // {
            //     PlayAudioClip( reticleAudioSource, badHighlightSound );
            //     loopingAudioSource.volume = 0.0f;
            // }
        }
        // Pointing at the same interactable
        else if (targetInteractable != null)
        {
            if (Vector3.Distance(prevPointedAtPosition, pointedAtPosition) > 1.0f)
            {
                prevPointedAtPosition = pointedAtPosition;
                PlayPointerHaptic(true); //!hitInteractable.locked );
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

