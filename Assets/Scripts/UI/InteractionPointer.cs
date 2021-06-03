
using UnityEngine;
using Valve.VR.InteractionSystem;
using Valve.VR;
using Swordfish;
using Swordfish.Audio;
using Swordfish.Navigation;
using System.Collections.Generic;
public class InteractionPointer : MonoBehaviour
{
    //=========================================================================
    [Header("Actions")]
	public SteamVR_Action_Boolean uiInteractAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI");
	public SteamVR_Action_Boolean selectAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Select");
	public SteamVR_Action_Boolean cancelAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Cancel");
	public SteamVR_Action_Boolean queueAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Queue");
	public SteamVR_Action_Boolean dequeueAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Dequeue");
	public SteamVR_Action_Boolean rotateBuildingClockwise = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("RotateBuildingClockwise");
	public SteamVR_Action_Boolean rotateBuildingCounterclockwise = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("RotateBuildingCounterclockwise");
	public SteamVR_Action_Boolean teleportAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport");

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
    [Header( "Audio Sources" )]
	public AudioSource pointerAudioSource;
	public AudioSource loopingAudioSource;
	public AudioSource headAudioSource;
	public AudioSource reticleAudioSource;

    //=========================================================================
    [Header( "Sounds" )]
	public SoundElement setRallyPointSound;
	public AudioClip queueSuccessSound;
    public AudioClip queueFailedSound;
    public AudioClip teleportSound;
	public AudioClip pointerLoopSound;
	public AudioClip pointerStopSound;
	public AudioClip goodHighlightSound;
	public AudioClip badHighlightSound;

	//=========================================================================
    private LineRenderer pointerLineRenderer;
	private LineRenderer[] lineRenderers;
	private GameObject pointerObject;
	private Transform pointerStartTransform;
	public float teleportFadeTime = 0.1f;
	public Hand pointerHand = null;
	private Player player = null;
	private TeleportArc teleportArc = null;
	public bool visible = false;
	private PointerInteractable pointedAtPointerInteractable;
	private	BuildingSpawnQueue buildingSpawnQueue;
	private List<Unit> selectedUnits;
	private Vector3 pointedAtPosition;
	private Vector3 prevPointedAtPosition;
	private float pointerShowStartTime = 0.0f;
	private float pointerHideStartTime = 0.0f;
	private bool meshFading = false;
	private float fullTintAlpha;
	private float invalidReticleMinScale = 0.2f;
	private float invalidReticleMaxScale = 1.0f;
	private float loopingAudioMaxVolume = 0.0f;
	private AllowTeleportWhileAttachedToHand allowTeleportWhileAttached = null;
	private Vector3 startingFeetOffset = Vector3.zero;
	private bool movedFeetFarEnough = false;
	public Hand handReticle;
	public bool useHandAsReticle;
	private bool teleporting = false;
	private float currentFadeTime = 0.0f;
	public GameObject wayPointReticle;
	private Resource pointedAtResource;
	private Vector3 rallyWaypointArcStartPosition;

	// Cache value
	private int maxUnitSelectionCount;

    // Cache value
    private Faction faction;
	private byte factionId;
    private PlayerManager playerManager;

    //=========================================================================
	// Modes
    private bool isInUnitSelectiodMode;
	private bool isInBuildingPlacementMode;
    private bool isInWallPlacementMode;
    private bool isSettingRallyPoint;

    //=========================================================================
    [Header("Building Placement")]
    public float buildingPlacementRotationIncrement = 45.0f;
    public float wallPlacementRotationIncrement = 22.5f;
    public bool placementStarted;
    public Hand placementHand;
    public bool placementEnded;
    private GameObject buildingPlacementPreviewObject;
	private float lastBuildingRotation;
    private BuildingData placementBuildingData;
    
    //=========================================================================
    [Header("Walls")]
    public GameObject woodWallWorld_1x1_Diagonal;
    public GameObject woodWallWorld_1x1_Diagonal_Preview;
    public GameObject stoneWallWorld_1x1_Diagonal;
    public GameObject stoneWallWorld_1x1_Diagonal_Preview;
	private GameObject wallPlacementPreviewStartObject;

    //=========================================================================
	// Cached wall objects
    private GameObject wallWorld_1x1;
	private GameObject wallWorld_1x1_Preview;
    private GameObject wallWorld_1x1_Diagonal;
    private GameObject wallWorld_1x1_Diagonal_Preview;

    private List<GameObject> wallPreviewSections = new List<GameObject>();
    private Swordfish.Coord2D lastPreviewPointerPosition;



    //=========================================================================
    private static InteractionPointer _instance;
	public static InteractionPointer instance
	{
		get
		{
			if ( _instance == null )
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

		// loopingAudioMaxVolume = loopingAudioSource.volume;

		// float invalidReticleStartingScale = invalidReticleTransform.localScale.x;
		// invalidReticleMinScale *= invalidReticleStartingScale;
		// invalidReticleMaxScale *= invalidReticleStartingScale;
	}

	//=========================================================================
	void Start()
	{
		HookIntoEvents();

        playerManager = PlayerManager.instance;

        //interactableObjects = GameObject.FindObjectsOfType<PointerInteractable>();
		selectedUnits = new List<Unit>();

		// Cache some values, going to need them a lot and don't need to keep
		// bothering the GameMaster/PlayerManager for them.
		maxUnitSelectionCount = GameMaster.Instance.maximumUnitSelectionCount;
		factionId = playerManager.factionId;

		// Setup LineRenderers for unit selection
		lineRenderers = new LineRenderer[maxUnitSelectionCount];
		for(int i = 0; i < maxUnitSelectionCount; i++)
		{
			lineRenderers[i] = Instantiate(pointerLineRenderer, this.transform);
			lineRenderers[i].enabled = false;
		}

		player = Valve.VR.InteractionSystem.Player.instance;

		if ( player == null )
		{
			Debug.LogError("<b>[SteamVR Interaction]</b> ObjectPlacementPointer: No Player instance found in map.", this);
			Destroy( this.gameObject );
			return;
		}

        ShowPointer();
	}

	//=========================================================================
	void Update()
	{
		// If something is attached to the hand that is preventing objectPlacement
		if ( allowTeleportWhileAttached && !allowTeleportWhileAttached.teleportAllowed )
		{
			//HidePointer();
		}

		//UpdatePointer();

		// if ( visible )
			UpdatePointer();
		// else
		// 	ShowPointer();

		Hand oldPointerHand = pointerHand;
		Hand newPointerHand = null;

		foreach (Hand hand in player.hands)
		{
			if (WasTeleportButtonReleased(hand))
				if (pointerHand == hand) //This is the pointer hand
					TryTeleportPlayer();

			if (WasTeleportButtonPressed(hand))
				newPointerHand = hand;

			//hand.uiInteractAction.GetStateDown(hand.handType)

			if (WasInteractButtonReleased(hand))
				if (pointerHand == hand)
					ExecuteInteraction();

            if (WasInteractButtonPressed(hand))
            {
                newPointerHand = hand;
                StartInteraction(hand);
            }

            if (WasQueueButtonPressed(hand))
				newPointerHand = hand;

            if (WasQueueButtonReleased(hand) && pointedAtPointerInteractable)
            {
                if (pointerHand == hand)
                {
                    BuildingSpawnQueue buildingSpawnQueue = pointedAtPointerInteractable.GetComponentInChildren<BuildingSpawnQueue>();
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
					BuildingSpawnQueue buildingSpawnQueue = pointedAtPointerInteractable.GetComponentInChildren<BuildingSpawnQueue>();
                    if (buildingSpawnQueue)
                    {
                        buildingSpawnQueue.DequeueUnit();
                        PlayAudioClip(headAudioSource, queueSuccessSound);
                    }
                }
			}

			if (WasCancelButtonPressed(hand))
			{
				if (isInUnitSelectiodMode)
                    EndUnitSelectionMode();
				else if (isInBuildingPlacementMode)
                    EndBuildingPlacementMode();
            }

			// 	if (isSettingRallyPoint)
			// 	{
            //         isSettingRallyPoint = false;
            //     }
			// }

			if (WasSelectButtonPressed(hand))
                newPointerHand = hand;

            if (WasSelectButtonReleased(hand))
				if (pointerHand == hand)
				{}

            if (isInBuildingPlacementMode)
			{
                // TODO: Should gates snap to nearby walls without having to be exactly lined
                // TODO: up with the wall?
                float rotationIncrement = buildingPlacementRotationIncrement;
                if (placementBuildingData.buildingType == RTSBuildingType.Wood_Wall_Gate ||
                	placementBuildingData.buildingType == RTSBuildingType.Stone_Wall_Gate)
                    rotationIncrement = 22.5f;

                if (WasRotateClockwiseButtonPressed(hand))
					buildingPlacementPreviewObject.transform.Rotate(0.0f, 0.0f, rotationIncrement);

				if (WasRotateCounterclockwiseButtonPressed(hand))
					buildingPlacementPreviewObject.transform.Rotate(0.0f, 0.0f, -rotationIncrement);

				// TODO: Should probably be moved to update pointer.
				HardSnapToGrid(destinationReticleTransform, placementBuildingData.boundingDimensionX, placementBuildingData.boundingDimensionY);
			}
		}
	}	

	private bool WasInteractButtonPressed(Hand hand)
	{
		if (CanInteract(hand))
			if (uiInteractAction.GetStateDown(hand.handType))
				return true;

		// Make sure it's off.
		if (wayPointReticle.activeSelf)
			wayPointReticle.SetActive(false);

		return false;
	}

	private bool WasInteractButtonReleased(Hand hand)
	{
		if (CanInteract(hand))
			if (uiInteractAction.GetStateUp(hand.handType))
				return true;

		// Make sure it's off.
		if (wayPointReticle.activeSelf)
			wayPointReticle.SetActive(false);

		return false;
	}

    private void StartInteraction(Hand hand)
	{
		if (isInWallPlacementMode)
		{
			if (!wallPlacementPreviewStartObject)
            {
				wallPlacementPreviewStartObject = Instantiate(buildingPlacementPreviewObject);
				wallPlacementPreviewStartObject.transform.Rotate(0, 0, lastBuildingRotation);
				wallPlacementPreviewStartObject.transform.position = buildingPlacementPreviewObject.transform.position;
			}

            if (placementBuildingData.buildingType == RTSBuildingType.Wood_Wall_Corner)
            {
				wallWorld_1x1 = GameMaster.GetBuilding(RTSBuildingType.Wood_Wall_1x1).worldPrefab;
                wallWorld_1x1_Preview = GameMaster.GetBuilding(RTSBuildingType.Wood_Wall_1x1).worldPreviewPrefab;
                wallWorld_1x1_Diagonal = woodWallWorld_1x1_Diagonal;
                wallWorld_1x1_Diagonal_Preview = woodWallWorld_1x1_Diagonal_Preview;
            }
			else if (placementBuildingData.buildingType == RTSBuildingType.Stone_Wall_Corner)
			{
                wallWorld_1x1 = GameMaster.GetBuilding(RTSBuildingType.Stone_Wall_1x1).worldPrefab;
                wallWorld_1x1_Preview = GameMaster.GetBuilding(RTSBuildingType.Stone_Wall_1x1).worldPreviewPrefab;
                wallWorld_1x1_Diagonal = stoneWallWorld_1x1_Diagonal;
                wallWorld_1x1_Diagonal_Preview = stoneWallWorld_1x1_Diagonal_Preview;
			}
        }
		else if (pointedAtPointerInteractable != null)
		{
			buildingSpawnQueue = pointedAtPointerInteractable.GetComponentInChildren<BuildingSpawnQueue>();

			if (buildingSpawnQueue && buildingSpawnQueue.enabled && !isSettingRallyPoint)
			{
				rallyWaypointArcStartPosition = pointedAtPointerInteractable.transform.position;
				isSettingRallyPoint = true;
				wayPointReticle.SetActive(true);
				return;
			}

			Unit hoveredUnit = pointedAtPointerInteractable.GetComponent<Unit>();
			if (hoveredUnit && !isInUnitSelectiodMode &&
				hoveredUnit.IsSameFaction(factionId))
			{
				selectedUnits.Add(hoveredUnit);
				isInUnitSelectiodMode = true;
				return;
			}

			//Debug.Log(string.Format("Unit: {0} interactable: {1}", selectedUnit, pointedAtPointerInteractable));
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
			Swordfish.Coord2D previousSegmentPosition = World.ToWorldCoord(wallPlacementPreviewStartObject.transform.position);

			// TODO: Instantiate start/end world pieces in place of preview pieces.
			foreach (GameObject go in wallPreviewSections)
			{
				Swordfish.Coord2D currentSegmentPosition = World.ToWorldCoord(go.transform.position);
                int currentIndex = wallPreviewSections.IndexOf(go);
                currentIndex++;
                GameObject nextObject = null;
                Swordfish.Coord2D nextSegmentPosition = null;

                if (currentIndex < wallPreviewSections.Count - 1)
                {
                    nextObject = wallPreviewSections[currentIndex];
                    nextSegmentPosition = World.ToWorldCoord(nextObject.transform.position);
                }

                // Corner
                if (nextObject && 
					nextSegmentPosition.x != previousSegmentPosition.x && 
					nextSegmentPosition.y != previousSegmentPosition.y &&
					(currentSegmentPosition.x == previousSegmentPosition.x ||
					currentSegmentPosition.y == previousSegmentPosition.y))
                {
                    Instantiate(placementBuildingData.worldPreviewPrefab, World.ToTransformSpace(currentSegmentPosition), buildingPlacementPreviewObject.transform.rotation);
                }
				else
					CreateWallSegment(previousSegmentPosition, currentSegmentPosition, previousSegmentPosition, wallWorld_1x1, wallWorld_1x1_Diagonal);

				previousSegmentPosition = currentSegmentPosition;
			}

            Instantiate(placementBuildingData.worldPrefab, wallPlacementPreviewStartObject.transform.position, wallPlacementPreviewStartObject.transform.rotation);
            Instantiate(placementBuildingData.worldPrefab, buildingPlacementPreviewObject.transform.position, buildingPlacementPreviewObject.transform.rotation);
            
			EndWallPlacementMode();
        }
		else if (isInBuildingPlacementMode)
		{
			isInBuildingPlacementMode = false;
			Instantiate(placementBuildingData.constructablePrefab, buildingPlacementPreviewObject.transform.position, buildingPlacementPreviewObject.transform.rotation);

			lastBuildingRotation = buildingPlacementPreviewObject.transform.localRotation.eulerAngles.y;

			Destroy(buildingPlacementPreviewObject);
			buildingPlacementPreviewObject = null;

			SetSnapTurnEnabled(true, true);
		}

		if (isSettingRallyPoint)
		{
			buildingSpawnQueue.SetUnitRallyWaypoint(wayPointReticle.transform.position);
            PlayAudioClip(headAudioSource, setRallyPointSound.GetClip());
			wayPointReticle.SetActive(false);
			buildingSpawnQueue = null;
			isSettingRallyPoint = false;
			pointerLineRenderer.enabled = false;
			return;
		}

		if (selectedUnits.Count > 0)
		{
			foreach (Unit unit in selectedUnits)
			{
				if (unit is Villager)
				{
                    Villager villager = unit.GetComponent<Villager>();

                    if (pointedAtPointerInteractable)
                    {
                        Structure structure = pointedAtPointerInteractable.GetComponent<Structure>();
						if (structure)
                        {
                            villager.SetUnitTask(RTSUnitType.Builder);
                            villager.TrySetGoal(World.at(structure.gridPosition));
                            continue;
                        }

                        Constructible constructible = pointedAtPointerInteractable.GetComponent<Constructible>();
						if (constructible)
						{
                            villager.SetUnitTask(RTSUnitType.Builder);							
                            villager.TrySetGoal(World.at(constructible.gridPosition));
                            continue;
                        }
                        
                        Resource resource = pointedAtPointerInteractable.GetComponent<Resource>();
                        if (resource)
                        {
                            // Needed for fauna since fauna has an inactive resource component
                            // that doesn't have a grid position to be fetched.
                            Swordfish.Coord2D gridPosition = resource.gridPosition;

                            switch (resource.type)
                            {
                                case ResourceGatheringType.Gold:
                                    villager.SetUnitTask(RTSUnitType.GoldMiner);
                                    break;

                                case ResourceGatheringType.Grain:
                                    villager.SetUnitTask(RTSUnitType.Farmer);
                                    break;

                                case ResourceGatheringType.Stone:
                                    villager.SetUnitTask(RTSUnitType.StoneMiner);
                                    break;

                                case ResourceGatheringType.Wood:
                                    villager.SetUnitTask(RTSUnitType.Lumberjack);
                                    break;

                                case ResourceGatheringType.Berries:
                                    villager.SetUnitTask(RTSUnitType.Forager);
                                    break;

                                case ResourceGatheringType.Fish:
                                    villager.SetUnitTask(RTSUnitType.Fisherman);
                                    break;

                                case ResourceGatheringType.Meat:
                                    villager.SetUnitTask(RTSUnitType.Hunter);
                                    gridPosition = resource.GetComponent<Fauna>().gridPosition;
                                    break;
                            }

                            villager.TrySetGoal(World.at(resource.gridPosition));
                            continue;
                        }
                    }

					villager.GotoPosition(pointedAtPosition);
					continue;
				}

				// Military unit.
				else if (unit is Soldier)
				// if (!civilian)
				{
					if (pointedAtPointerInteractable)
					{
						Unit pointedAtUnit = pointedAtPointerInteractable.GetComponent<Unit>();

						// Not the same faction.
						if (pointedAtUnit && !unit.IsSameFaction(factionId))
						{
                            // TODO: Force attack unit/set target
                            // Attack unit
                            unit.TrySetGoal(World.at(World.ToWorldCoord(pointedAtPosition)));
							continue;
						}
						// Same faction, go to units position.
						else if (pointedAtUnit)
						{
                            unit.GotoPosition(pointedAtUnit.transform.position);
							continue;
						}
					}
					else
                    	// Default go to position.
                    	unit.GotoPosition(pointedAtPosition);
				}
			}

            // Cleanup
            EndUnitSelectionMode();
			pointedAtResource = null;
		}
	}

    private void EndBuildingPlacementMode()
	{
		isInBuildingPlacementMode = false;
		Destroy(buildingPlacementPreviewObject);
		buildingPlacementPreviewObject = null;

		// TODO: Restore resources to player

		SetSnapTurnEnabled(true, true);
	}

	private void ClearWallPreviewSections()
	{
		GameObject[] walls = wallPreviewSections.ToArray();
		for (int i = 0; i < walls.Length; i++)
		{
			Destroy(walls[i]);
		}

		wallPreviewSections.Clear();
	}

    private void DrawWallPreview()
	{
		// This is a corner piece that has been placed already when the
		// trigger was pressed, it is the beginning point of the wall.
		Vector3 pos1 = wallPlacementPreviewStartObject.transform.position;

		// This is the current reticle location with a corner wall preview
		// piece attached, this is the end of the wall.
		Vector3 pos2 = buildingPlacementPreviewObject.transform.position;

		// ! This is to reduce DrawWallPreview calls, commented out for testing.
		// if (lastPreviewPointerPosition == World.ToWorldCoord(pos2))
        //     return;
        // lastPreviewPointerPosition = World.ToWorldCoord(pos2);

		ClearWallPreviewSections();

		// Grid unit * wall bounding dimension
        float wallWorldLength = 0.125f * 1.0f;
		Vector3 dir = (pos2 - pos1).normalized;
		Vector3 segmentPos = pos1 + (dir * wallWorldLength);
        Swordfish.Coord2D startPosition = World.ToWorldCoord(pos1);
        Swordfish.Coord2D endPosition = World.ToWorldCoord(pos2);

        // Track the position of the previous wall segment so we can decide
        // the rotation of the next wall segment in relation to the previous
        // wall segment.
        Swordfish.Coord2D previousSegmentPosition = startPosition;
		Swordfish.Coord2D nextSegmentPosition = World.ToWorldCoord(segmentPos);
        Cell nextCellPosition = World.at(nextSegmentPosition);
        Swordfish.Coord2D difference = endPosition - startPosition;

		GameObject obj = null;

        int sizeX = Mathf.Abs(difference.x);
        int sizeY = Mathf.Abs(difference.y);

        // Diagonal from start position, draw 45 degree walls.
        if (sizeX == sizeY)
		{
            // Create 45 degree segments
            for (int i = 1; i < sizeX; ++i)
            {
				// ! CreateWallSegment works as it is for the final world wall generation
				// ! so bugs aren't from it. Has to be something else.
				//obj = CreateWallSegment(previousSegmentPosition, nextSegmentPosition, wallWorld_1x1_Preview, wallWorld_1x1_Diagonal_Preview);

				// ! Just an abbreviated CreateWallSegment that only deals with diagonals, or
				// ! atleast should.
				//obj = GetDiagonalWallRotated(previousSegmentPosition, nextSegmentPosition, wallWorld_1x1_Diagonal_Preview);

				obj = Instantiate(wallWorld_1x1_Preview, World.ToTransformSpace(nextSegmentPosition), buildingPlacementPreviewObject.transform.rotation);

				previousSegmentPosition = nextSegmentPosition;
				nextSegmentPosition.x += 1 * (int)Mathf.Sign(difference.x);
				nextSegmentPosition.y += 1 * (int)Mathf.Sign(difference.y);

                if (!obj)
                {
                    Debug.Log(string.Format(
                    "previousSegmentPosition: {0}, {1}  nextSegmentPosition: {2}, {3} ", previousSegmentPosition.x, previousSegmentPosition.y, nextSegmentPosition.x, nextSegmentPosition.y));
                    continue;
                }

                // Preview objects don't have buildingDimensions on the object
                // so we have to snap ourselves.
                HardSnapToGrid(obj.transform, 1, 1);

                wallPreviewSections.Add(obj);
            }
        }
		else
		{
            //-----------------------------------------------------------------
            // East - West (X axis)

			// Reset to start position
            nextSegmentPosition = startPosition;

			// Start at 1 because the first wall segment will be a corner piece,
			// not a wall piece.
            for (int i = 1; i < sizeX; ++i)
            {                
				nextSegmentPosition.x += 1 * (int)Mathf.Sign(difference.x);
                DrawWallSegmentPreview(nextSegmentPosition, 90.0f, wallWorld_1x1_Preview);
            }

			// Not a straight wall, need a corner
			if (endPosition.x != nextSegmentPosition.x && endPosition.y != nextSegmentPosition.y)
			{
				nextSegmentPosition.x += 1 * (int)Mathf.Sign(difference.x);
                
				Cell nextCell = World.at(nextSegmentPosition);
                if (!nextCell.occupied)
                {
                    obj = Instantiate(wallPlacementPreviewStartObject, World.ToTransformSpace(nextSegmentPosition), buildingPlacementPreviewObject.transform.rotation);
                    // obj.transform.Rotate(0, 0, 90);
                    HardSnapToGrid(obj.transform, 1, 1);
                    wallPreviewSections.Add(obj);
                }
                // nextCell.occupied = true
                else
                {
                    // Draw corner piece.
                    Structure structure = nextCell.GetFirstOccupant<Structure>();
                    if (structure?.buildingData.buildingType == RTSBuildingType.Wood_Wall_1x1 ||
                        structure?.buildingData.buildingType == RTSBuildingType.Stone_Wall_1x1 ||
                        structure?.buildingData.buildingType == RTSBuildingType.Wood_Wall_Corner ||
                		structure?.buildingData.buildingType == RTSBuildingType.Stone_Wall_Corner)
                    {
                        segmentPos = World.ToTransformSpace(nextSegmentPosition);
                        obj = Instantiate(placementBuildingData.worldPreviewPrefab, segmentPos, buildingPlacementPreviewObject.transform.rotation);
                        HardSnapToGrid(obj.transform, 1, 1);
                        wallPreviewSections.Add(obj);
                    }
                }
            }

			//-----------------------------------------------------------------
			// North - South (Y axis)

			// Reset to start position
            nextSegmentPosition = startPosition;
			nextSegmentPosition.x = endPosition.x;

			// Start at 1 because the first wall segment will be a corner piece,
			// not a wall piece.
            for (int i = 1; i < sizeY; ++i)
            {
                nextSegmentPosition.y += 1 * (int)Mathf.Sign(difference.y);
                DrawWallSegmentPreview(nextSegmentPosition, 0.0f, wallWorld_1x1_Preview);
            }
        }
    }	

	private void DrawWallSegmentPreview(Coord2D segmentPosition, float wallRotation, GameObject wallPrefab)
	{
        Vector3 segmentPos = Vector3.zero;
        GameObject obj = null;

        Cell nextCell = World.at(segmentPosition);
        if (!nextCell.occupied)
        {
            segmentPos = World.ToTransformSpace(segmentPosition);
            obj = Instantiate(wallPrefab, segmentPos, buildingPlacementPreviewObject.transform.rotation);
            obj.transform.Rotate(0, 0, wallRotation);
        } // End !nextCell.occupied

        // nextCell.occupied = true
        else
        {
            // Draw a corner piece.
            Structure structure = nextCell.GetFirstOccupant<Structure>();
            if (structure?.buildingData.buildingType == RTSBuildingType.Wood_Wall_1x1 ||
                structure?.buildingData.buildingType == RTSBuildingType.Stone_Wall_1x1 ||
                structure?.buildingData.buildingType == RTSBuildingType.Wood_Wall_Corner ||
                structure?.buildingData.buildingType == RTSBuildingType.Stone_Wall_Corner)
            {
                segmentPos = World.ToTransformSpace(segmentPosition);
                obj = Instantiate(placementBuildingData.worldPreviewPrefab, segmentPos, buildingPlacementPreviewObject.transform.rotation);
            }
            else
                return;
        }

        HardSnapToGrid(obj.transform, 1, 1);
        wallPreviewSections.Add(obj);
	}
	private GameObject GetDiagonalWallRotated(Coord2D previousPosition, Coord2D nextPosition, GameObject diagonalWall)
	{
        GameObject obj = null;

		Vector3 segmentPos = World.ToTransformSpace(nextPosition);

		// Southeast
		if (nextPosition.x > previousPosition.x && nextPosition.y < previousPosition.y)
		{
			obj = Instantiate(diagonalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
			obj.transform.Rotate(0, 0, 90);
		}
		// Northwest
		else if (nextPosition.x < previousPosition.x && nextPosition.y > previousPosition.y)
		{
            obj = Instantiate(diagonalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
			obj.transform.Rotate(0, 0, 90);
		}
		else
		 	obj = Instantiate(diagonalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);

		return obj;
    }

    private GameObject CreateWallSegment(Coord2D lastPosition, Coord2D currentPosition, Coord2D nextPosition, GameObject normalWall, GameObject diagonalWall)
    {
        GameObject obj = null;

        Vector3 segmentPos = World.ToTransformSpace(currentPosition);

        //---------------------------------------------------------------------
        // Occupied cell
        Cell nextCell = World.at(currentPosition);
        if (nextCell.occupied)
        {
            // Replace the 1x1 with a corner if this is a 1x1 wall segment.
            Structure structure = nextCell.GetFirstOccupant<Structure>();
            if (structure?.buildingData.buildingType == RTSBuildingType.Wood_Wall_1x1 ||
                structure?.buildingData.buildingType == RTSBuildingType.Stone_Wall_1x1 ||
                structure?.buildingData.buildingType == RTSBuildingType.Wood_Wall_Corner ||
                structure?.buildingData.buildingType == RTSBuildingType.Stone_Wall_Corner)
            {
                structure.UnbakeFromGrid();
                structure.gameObject.SetActive(false);
                Destroy(structure);

                segmentPos = World.ToTransformSpace(currentPosition);
                obj = Instantiate(placementBuildingData.worldPreviewPrefab, segmentPos, buildingPlacementPreviewObject.transform.rotation);
            }
            // Space is occupied, do nothing.
            else
                return obj;
        }		

        //---------------------------------------------------------------------
        // Eastward (nextPosition.x > lastPosition.x)
        // East
        else if (currentPosition.x > lastPosition.x && currentPosition.y == lastPosition.y)
		{
			obj = Instantiate(normalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
			obj.transform.Rotate(0, 0, 90);
		}
		// Southeast
		else if (currentPosition.x > lastPosition.x && currentPosition.y < lastPosition.y)
		{
			obj = Instantiate(diagonalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
			obj.transform.Rotate(0, 0, 90);
		}
		// Northeast
		else if (currentPosition.x > lastPosition.x && currentPosition.y > lastPosition.y)
		{
			obj = Instantiate(diagonalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
		}

		//---------------------------------------------------------------------
		// Westward (nextPosition.x < lastPosition.x)
		// West
		else if (currentPosition.x < lastPosition.x && currentPosition.y == lastPosition.y)
		{
			obj = Instantiate(normalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
			obj.transform.Rotate(0, 0, 90);
		}
		// Southwest
		else if (currentPosition.x < lastPosition.x && currentPosition.y < lastPosition.y)
		{
			obj = Instantiate(diagonalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
		}
		// Northwest
		else if (currentPosition.x < lastPosition.x && currentPosition.y > lastPosition.y)
		{
            obj = Instantiate(diagonalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
			obj.transform.Rotate(0, 0, 90);
		}

		//---------------------------------------------------------------------
		// South
		else if (currentPosition.y > lastPosition.y)
		{
			obj = Instantiate(normalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
		}

		//---------------------------------------------------------------------
		// North
		else if (currentPosition.y < lastPosition.y)
		{
			obj = Instantiate(normalWall, segmentPos, buildingPlacementPreviewObject.transform.rotation);
		}

        return obj;
    }

	private void EndUnitSelectionMode()
	{
		isInUnitSelectiodMode = false;
		pointedAtResource = null;
		selectedUnits.Clear();
		foreach(LineRenderer lineRenderer in lineRenderers)
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

		//Check pointer angle
		float dotUp = Vector3.Dot( pointerDir, Vector3.up );
		float dotForward = Vector3.Dot( pointerDir, player.hmdTransform.forward );
		bool pointerAtBadAngle = false;

		if ((dotForward > 0 && dotUp > 0.75f) || (dotForward < 0.0f && dotUp > 0.5f))
			pointerAtBadAngle = true;

		//Trace to see if the pointer hit anything
		RaycastHit hitInfo;
		teleportArc.SetArcData( pointerStart, arcVelocity, true, pointerAtBadAngle );

		teleportArc.FindProjectileCollision( out hitInfo );
		//if ( teleportArc.DrawArc( out hitInfo ) )
		if (hitInfo.collider)
		{
			hitSomething = true;
			hitPointValid = LayerMatchTest(allowedPlacementLayers, hitInfo.collider.gameObject);

			if (selectedUnits.Count > 0)
				pointedAtResource = hitInfo.collider.GetComponentInParent<Resource>();

			hitPointerInteractable = hitInfo.collider.GetComponent<PointerInteractable>();
			if (!hitPointerInteractable)
				hitPointerInteractable = hitInfo.collider.GetComponentInParent<PointerInteractable>();
		}

		HighlightSelected( hitPointerInteractable );

		if (hitPointerInteractable != null)
			pointedAtPointerInteractable = hitPointerInteractable;
		else
			pointedAtPointerInteractable = null;

		pointedAtPosition = hitInfo.point;
		pointerEnd = hitInfo.point;

		if ( hitSomething )
			pointerEnd = hitInfo.point;
		else
			pointerEnd = teleportArc.GetArcPositionAtTime( teleportArc.arcDuration );

		destinationReticleTransform.position = pointedAtPosition;
		destinationReticleTransform.gameObject.SetActive( true );

		if (isSettingRallyPoint)
		{
			DrawQuadraticBezierCurve(pointerLineRenderer, rallyWaypointArcStartPosition, pointedAtPosition);
			if (pointerLineRenderer.enabled == false)
				pointerLineRenderer.enabled = true;

		}
		else if (isInUnitSelectiodMode && pointedAtPointerInteractable != null)
		{
			Unit hoveredUnit = pointedAtPointerInteractable.GetComponent<Unit>();
			if (hoveredUnit && !selectedUnits.Contains(hoveredUnit) &&
				factionId == hoveredUnit.factionId)
			{
				selectedUnits.Add(hoveredUnit);
			}
		}
		else if (isInBuildingPlacementMode)
		{
			DrawQuadraticBezierCurve(pointerLineRenderer, pointerStart, destinationReticleTransform.position);
			if (pointerLineRenderer.enabled == false)
				pointerLineRenderer.enabled = true;
		}
		else if (isInWallPlacementMode)
		{
			HardSnapToGrid(destinationReticleTransform, placementBuildingData.boundingDimensionX, placementBuildingData.boundingDimensionY);
            if (wallPlacementPreviewStartObject)// && buildingPlacementPreviewObject)
            {
				// ! Choose a method to use....
                DrawWallPreview(); // AOE2 style, 45's don't work.
				//DrawWallPreview2();  // No corners ala AOE2, 45's are buggy.
            }

            //DrawQuadraticBezierCurve(pointerLineRenderer, pointerStart, destinationReticleTransform.position);
            if (pointerLineRenderer.enabled == false)
				pointerLineRenderer.enabled = true;
		}
		else
			if (pointerLineRenderer.enabled == true)
				pointerLineRenderer.enabled = false;

		if (selectedUnits.Count > 0)
		{
			int i = 0;
			foreach (Unit unit in selectedUnits)
			{
				LineRenderer lineRenderer = lineRenderers[i];

				if (!unit)
				{
					selectedUnits.Remove(unit);
					if (lineRenderers[i].enabled)
						lineRenderers[i].enabled = false;
				}
				else
				{
					DrawQuadraticBezierCurve(lineRenderers[i], unit.transform.position, pointedAtPosition);
					if (!lineRenderers[i].enabled)
						lineRenderers[i].enabled = true;
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
		// ! Want to eventually switch action maps based on activity.
		// SteamVR_Actions.construction.Activate();

		SetSnapTurnEnabled(false, false);
		placementBuildingData = e.buildingData;

		if (placementBuildingData.buildingType == RTSBuildingType.Wood_Wall_Corner)
		{
			isInWallPlacementMode = true;
            BuildingData buildingData = GameMaster.GetBuilding(RTSBuildingType.Wood_Wall_Corner);
            buildingPlacementPreviewObject = Instantiate(buildingData.worldPreviewPrefab, destinationReticleTransform);
        }
        else if (placementBuildingData.buildingType == RTSBuildingType.Stone_Wall_Corner)
        {
            isInWallPlacementMode = true;
            BuildingData buildingData = GameMaster.GetBuilding(RTSBuildingType.Stone_Wall_Corner);
            buildingPlacementPreviewObject = Instantiate(buildingData.worldPreviewPrefab, destinationReticleTransform);
        }
        else
        {
            isInBuildingPlacementMode = true;
			buildingPlacementPreviewObject = Instantiate(placementBuildingData.worldPreviewPrefab, destinationReticleTransform);
            buildingPlacementPreviewObject.transform.Rotate(0, 0, lastBuildingRotation);
        }

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
        if (wallPlacementPreviewStartObject)
	        Destroy(wallPlacementPreviewStartObject);

        if (buildingPlacementPreviewObject)
	        Destroy(buildingPlacementPreviewObject);

        wallPlacementPreviewStartObject = null;
        buildingPlacementPreviewObject = null;

        SetSnapTurnEnabled(true, true);
    }

	private bool CanInteract( Hand hand)
	{
		// !PlayerManager.instance.handBuildMenu.activeSelf
		if (!hand.currentAttachedObject && !hand.hoveringInteractable)
			return true;

		return false;
	}

	private bool WasTeleportButtonReleased( Hand hand )
	{
		if ( IsEligibleForTeleport( hand ) )
		{
			if ( hand.noSteamVRFallbackCamera != null )
				return Input.GetKeyUp( KeyCode.T );
			else
				return teleportAction.GetStateUp(hand.handType);
		}

		return false;
	}


	public bool IsEligibleForTeleport( Hand hand )
	{
		// TODO: Clean this up so it works for both hands. Ideally, just have different action
		// sets.
		if (isInBuildingPlacementMode && hand.handType == SteamVR_Input_Sources.RightHand)
			return false;

		if ( hand == null )
			return false;

		if ( !hand.gameObject.activeInHierarchy )
			return false;

		if ( hand.hoveringInteractable != null )
			return false;

		if ( hand.noSteamVRFallbackCamera == null )
		{
			if ( hand.isActive == false)
				return false;

			//Something is attached to the hand
			if ( hand.currentAttachedObject != null )
			{
				AllowTeleportWhileAttachedToHand allowTeleportWhileAttachedToHand = hand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();

				if ( allowTeleportWhileAttachedToHand != null && allowTeleportWhileAttachedToHand.teleportAllowed == true )
					return true;
				else
					return false;
			}
		}

		return true;
	}

	private bool WasTeleportButtonPressed( Hand hand )
	{
		if ( IsEligibleForTeleport( hand ) )
		{
			if ( hand.noSteamVRFallbackCamera != null )
				return Input.GetKeyDown( KeyCode.T );
			else
				return teleportAction.GetStateDown(hand.handType);
				//return hand.controller.GetPressDown( SteamVR_Controller.ButtonMask.Touchpad );
		}

		return false;
	}


	private void TryTeleportPlayer()
	{
		if ( !teleporting )
		{
			// TODO: Change this code to use buildings as teleportmarkers and when
			// teleporting to buildings the menu for them is possible displayed if
			// it has a build/upgrade menu.

			// if ( pointedAtTeleportMarker != null && pointedAtTeleportMarker.locked == false )
			// {
				//Pointing at an unlocked teleport marker
				//teleportingToMarker = pointedAtTeleportMarker;

				InitiateTeleportFade();
				//CancelTeleportHint();
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

		SteamVR_Fade.Start( Color.clear, 0 );
		SteamVR_Fade.Start( Color.black, currentFadeTime );

		headAudioSource.transform.SetParent( player.hmdTransform );
		headAudioSource.transform.localPosition = Vector3.zero;
		PlayAudioClip( headAudioSource, teleportSound );

		Invoke( "TeleportPlayer", currentFadeTime );
	}

	private void TeleportPlayer()
		{
			teleporting = false;
			SteamVR_Fade.Start( Color.clear, currentFadeTime );
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
		return ( ( 1 << obj.layer ) & layerMask ) != 0;
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
		if ( visible )
			pointerHideStartTime = Time.time;

		visible = false;
		//pointerObject.SetActive( false );
		teleportArc.Hide();
	}


	//=========================================================================
	private void ShowPointer()
	{
		if ( !visible )
		{
			pointedAtPointerInteractable = null;
			pointerShowStartTime = Time.time;
			visible = true;
			//pointerObject.SetActive( false );
			teleportArc.Show();

			// foreach ( PointerInteractable interactObject in interactableObjects )
			// 	interactObject.Highlight( false );

			startingFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
			movedFeetFarEnough = false;
		}

		pointerStartTransform = pointerAttachmentPoint.transform;

		if ( pointerHand.currentAttachedObject != null )
		{
			//allowTeleportWhileAttached = pointerHand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();
		}
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
        return rotateBuildingClockwise.GetStateDown(hand.handType);
    }

    private bool WasRotateCounterclockwiseButtonPressed(Hand hand)
    {
        return rotateBuildingCounterclockwise.GetStateDown(hand.handType);
    }

    private bool WasSelectButtonPressed(Hand hand)
    {
        return selectAction.GetStateDown(hand.handType);
    }

    private bool WasCancelButtonPressed(Hand hand)
    {
        return cancelAction.GetStateDown(hand.handType);
    }

    private void SetSnapTurnEnabled(bool right, bool left)
    {
        Player.instance.GetComponentInChildren<SnapTurn>().rightHandEnabled = right;
        Player.instance.GetComponentInChildren<SnapTurn>().leftHandEnabled = left;
        Player.instance.GetComponentInChildren<SnapTurn>().enabled = right && left;
    }

    public void HardSnapToGrid(Transform obj, int boundingDimensionX, int boundingDimensionY)
    {
        Vector3 pos = World.ToWorldSpace(obj.position);

        obj.position = World.ToTransformSpace(new Vector3(Mathf.RoundToInt(pos.x), obj.position.y, Mathf.RoundToInt(pos.z)));

        Vector3 modPos = obj.position;
        if (boundingDimensionX % 2 == 0)
            modPos.x = obj.position.x + World.GetUnit() * -0.5f;

        if (boundingDimensionY % 2 == 0)
            modPos.z = obj.position.z + World.GetUnit() * -0.5f;

        obj.position = modPos;
    }

	//=========================================================================
	private void PlayAudioClip( AudioSource source, AudioClip clip )
	{
		source.clip = clip;
		source.Play();
	}


	//=========================================================================
	private void PlayPointerHaptic( bool validLocation )
	{
		if ( pointerHand != null )
			if ( validLocation )
				pointerHand.TriggerHapticPulse( 800 );
			else
				pointerHand.TriggerHapticPulse( 100 );
	}


	private void HighlightSelected( PointerInteractable hitPointerInteractable )
	{
		// Pointing at a new interactable
		if ( pointedAtPointerInteractable != hitPointerInteractable )
		{
			if ( pointedAtPointerInteractable != null )
				pointedAtPointerInteractable.Highlight( false );

			if ( hitPointerInteractable != null )
			{
				hitPointerInteractable.Highlight( true );
				prevPointedAtPosition = pointedAtPosition;
				PlayPointerHaptic( true );//!hitPointerInteractable.locked );
				// PlayAudioClip( reticleAudioSource, goodHighlightSound );
				// loopingAudioSource.volume = loopingAudioMaxVolume;
			}
			else if ( pointedAtPointerInteractable != null )
			{
				// PlayAudioClip( reticleAudioSource, badHighlightSound );
				// loopingAudioSource.volume = 0.0f;
			}
		}
		// Pointing at the same interactable
		else if ( hitPointerInteractable != null )
		{
			if ( Vector3.Distance( prevPointedAtPosition, pointedAtPosition ) > 1.0f )
			{
				prevPointedAtPosition = pointedAtPosition;
				PlayPointerHaptic( true ); //!hitPointerInteractable.locked );
			}
		}
	}

	//=========================================================================
	private bool ShouldOverrideHoverLock()
	{
		if ( !allowTeleportWhileAttached || allowTeleportWhileAttached.overrideHoverLock )
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

	public void StartPlacement(Hand hand)
	{
		placementEnded = false;
		placementStarted = true;
	}

	public void StopPlacement(Hand hand)
	{
		placementStarted = false;
		placementEnded = true;
		visible = false;
		HidePointer();
	}
}

