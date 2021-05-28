﻿
using UnityEngine;
using Valve.VR.InteractionSystem;
using Valve.VR;
using Swordfish.Audio;
using Swordfish.Navigation;
using System.Collections.Generic;

public class InteractionPointer : MonoBehaviour
{
	public SteamVR_Action_Boolean uiInteractAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InteractUI");
	public SteamVR_Action_Boolean selectAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Select");
	public SteamVR_Action_Boolean cancelAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Cancel");
	public SteamVR_Action_Boolean queueAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Queue");
	public SteamVR_Action_Boolean dequeueAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Dequeue");
	public SteamVR_Action_Boolean rotateBuildingClockwise = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("RotateBuildingClockwise");
	public SteamVR_Action_Boolean rotateBuildingCounterclockwise = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("RotateBuildingCounterclockwise");

	public SteamVR_Action_Boolean teleportAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Teleport");

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

	[Header( "Audio Sources" )]
	public AudioSource pointerAudioSource;
	public AudioSource loopingAudioSource;
	public AudioSource headAudioSource;
	public AudioSource reticleAudioSource;

	[Header( "Sounds" )]
	public SoundElement setRallyPointSound;
	public AudioClip teleportSound;
	public AudioClip pointerLoopSound;
	public AudioClip pointerStopSound;
	public AudioClip goodHighlightSound;
	public AudioClip badHighlightSound;


	private LineRenderer pointerLineRenderer;
	private LineRenderer[] lineRenderers;
	private GameObject pointerObject;
	private Transform pointerStartTransform;
	public float teleportFadeTime = 0.1f;
	public Hand pointerHand = null;
	private Player player = null;
	private TeleportArc teleportArc = null;
	public bool visible = false;
	private PointerInteractable[] interactableObjects;
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
	
	public bool placementStarted;
	public Hand placementHand;
	public bool placementEnded;

	public GameObject wayPointReticle;
	private Resource pointedAtResource;
	private Vector3 rallyWaypointArcStartPosition;
	bool isSettingRallyPoint;
	
	private int maxUnitSelectionCount;
	private byte teamId;
    private PlayerManager playerManager;

    //-------------------------------------------------
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

	//-------------------------------------------------
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


	//-------------------------------------------------
	void Start()
	{
		HookIntoEvents();

        playerManager = PlayerManager.instance;

        // Used to un-highlight things
        interactableObjects = GameObject.FindObjectsOfType<PointerInteractable>();
		
		selectedUnits = new List<Unit>();

		// Cache some values, going to need them a lot and don't need to keep
		// bothering the GameMaster/PlayerManager for them.
		maxUnitSelectionCount = GameMaster.Instance.maximumUnitSelectionCount;
		teamId = playerManager.factionId;

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

	

	//-------------------------------------------------
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

		foreach ( Hand hand in player.hands )
		{
			if ( WasTeleportButtonReleased( hand ) )
				if ( pointerHand == hand ) //This is the pointer hand
					TryTeleportPlayer();

			if ( WasTeleportButtonPressed( hand ) )
				newPointerHand = hand;

			//hand.uiInteractAction.GetStateDown(hand.handType)
			
			if (WasInteractButtonReleased(hand))
				if (pointerHand == hand)
					TryInteract();

			if ( WasInteractButtonPressed(hand))
				newPointerHand = hand;		

			if (WasQueueButtonPressed(hand))
				newPointerHand = hand;

			if (WasQueueButtonReleased(hand) && pointedAtPointerInteractable)
				if (pointerHand == hand)
				{
					BuildingSpawnQueue buildingSpawnQueue = pointedAtPointerInteractable.GetComponentInChildren<BuildingSpawnQueue>();
					if (buildingSpawnQueue)
						buildingSpawnQueue.QueueLastUnitQueued();
				}

			if (WasDequeueButtonPressed(hand))
				newPointerHand = hand;
				
			if (WasDequeueButtonReleased(hand) && pointedAtPointerInteractable)
			{
				if (pointerHand == hand)
				{
					BuildingSpawnQueue buildingSpawnQueue = pointedAtPointerInteractable.GetComponentInChildren<BuildingSpawnQueue>();
					if (buildingSpawnQueue)
						buildingSpawnQueue.DequeueUnit();
				}
			}

			if (isInBuildingPlacementMode)
			{
				if (WasRotateClockwiseButtonPressed(hand))
					buildingPlacementPreviewObject.transform.Rotate(0.0f, 0.0f, 45.0f);

				if (WasRotateCounterclockwiseButtonPressed(hand))
					buildingPlacementPreviewObject.transform.Rotate(0.0f, 0.0f, -45.0f);

				HardSnapToGrid(destinationReticleTransform, placementBuildingData);
				
				if (WasSelectButtonPressed(hand))
				{
					isInBuildingPlacementMode = false;		
					Instantiate(placementBuildingData.constructablePrefab, buildingPlacementPreviewObject.transform.position, buildingPlacementPreviewObject.transform.rotation);
					
					lastBuildingRotation = buildingPlacementPreviewObject.transform.localRotation.eulerAngles.y;

					Destroy(buildingPlacementPreviewObject);
					buildingPlacementPreviewObject = null;

					SetSnapTurnEnabled(true, true);
				}

				if (WasCancelButtonPressed(hand))
				{
					isInBuildingPlacementMode = false;	
					Destroy(buildingPlacementPreviewObject);
					buildingPlacementPreviewObject = null;
					
					// TODO: Restore resources to player

					SetSnapTurnEnabled(true, true);
				}
			}
		}
	}

	private void SetSnapTurnEnabled(bool right, bool left)
	{
		Player.instance.GetComponentInChildren<SnapTurn>().rightHandEnabled = right;
		Player.instance.GetComponentInChildren<SnapTurn>().leftHandEnabled = left;
		Player.instance.GetComponentInChildren<SnapTurn>().enabled = right && left;
	}

	private bool isInUnitSelectiodMode;
	private bool isInBuildingPlacementMode;
	private GameObject buildingPlacementPreviewObject;
	private float lastBuildingRotation;

	private bool WasQueueButtonPressed(Hand hand)
	{
		return queueAction.GetStateDown(hand.handType);
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

	private bool WasInteractButtonPressed(Hand hand)
	{
		// TODO: listen for different button to cancel
		if (CanInteract(hand))
		{
			if (uiInteractAction.GetStateDown(hand.handType))
			{
				if (pointedAtPointerInteractable != null)
				{
					buildingSpawnQueue = pointedAtPointerInteractable.GetComponentInChildren<BuildingSpawnQueue>();
					
					if (buildingSpawnQueue && buildingSpawnQueue.enabled && !isSettingRallyPoint)
					{ 
						rallyWaypointArcStartPosition = pointedAtPointerInteractable.transform.position;
						isSettingRallyPoint = true;	
						wayPointReticle.SetActive(true);
						return true;											
					}

					Unit hoveredUnit = pointedAtPointerInteractable.GetComponent<Unit>();
					if (hoveredUnit && !isInUnitSelectiodMode &&
						hoveredUnit.IsSameFaction(teamId))
					{
						selectedUnits.Add(hoveredUnit);
						isInUnitSelectiodMode = true;
						return true;
					}					

					//Debug.Log(string.Format("Unit: {0} interactable: {1}", selectedUnit, pointedAtPointerInteractable));
				}

				return true;
			}
		}

		// Make sure it's off.
		if (wayPointReticle.activeSelf)
			wayPointReticle.SetActive(false);

		return false;
	}
	
	private bool WasInteractButtonReleased(Hand hand)
	{
		if ( CanInteract(hand))
		{
			if (uiInteractAction.GetStateUp(hand.handType))
				return true;
		}

		// Make sure it's off.
		if (wayPointReticle.activeSelf)
			wayPointReticle.SetActive(false);

		return false;
	}

	private void TryInteract()
	{
		if (isSettingRallyPoint)
		{
			buildingSpawnQueue.SetUnitRallyWaypoint(wayPointReticle.transform.position);
			headAudioSource.PlayOneShot(setRallyPointSound.GetClip());
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
				bool civilian = unit.IsCivilian();

				// Villager unit and we're targeting a resource.
				if (unit is Villager && pointedAtResource)
				{
					Villager villager = unit.GetComponent<Villager>();
                    
					// Needed for fauna since fauna has an inactive resource component
					// that doesn't have a grid position to be fetched.
					Swordfish.Coord2D gridPosition = pointedAtResource.gridPosition;
                  
                    switch (pointedAtResource.type)
					{
						case ResourceGatheringType.Gold:
							villager.SetUnitType(RTSUnitType.GoldMiner);								
							break;

						case ResourceGatheringType.Grain:
							villager.SetUnitType(RTSUnitType.Farmer);										
							break;

						case ResourceGatheringType.Stone:
							villager.SetUnitType(RTSUnitType.StoneMiner);										
							break;

						case ResourceGatheringType.Wood:
							villager.SetUnitType(RTSUnitType.Lumberjack);										
							break;
						
						case ResourceGatheringType.Berries:
							villager.SetUnitType(RTSUnitType.Forager);										
							break;

						case ResourceGatheringType.Fish:
							villager.SetUnitType(RTSUnitType.Fisherman);										
							break;

						case ResourceGatheringType.Meat:
                            gridPosition = pointedAtResource.GetComponent<Fauna>().gridPosition;
                            villager.SetUnitType(RTSUnitType.Hunter);
                            break;
					}
                    
                    PathfindingGoal.TryGoal((Actor)villager, World.at(gridPosition), villager.GetGoals());
					villager.GotoForced(gridPosition.x, gridPosition.y);
					villager.ResetGoal();
					continue;
				}				

				// Military unit.
				if (unit is Soldier)
				// if (!civilian)
				{	
					if (pointedAtPointerInteractable)
					{
						Unit pointedAtUnit = pointedAtPointerInteractable.GetComponent<Unit>();

						// Not the same faction.
						if (pointedAtUnit && !unit.IsSameFaction(teamId))
						{
							// TODO: Force attack unit/set target
							// Attack unit
							unit.GotoForced(World.ToWorldSpace(pointedAtPosition));						
							unit.ResetGoal();						
							continue;
						}
						// Same faction, go to units position.
						else if (pointedAtUnit)
						{
							unit.GotoForced(unit.gridPosition.x, unit.gridPosition.y);						
							unit.ResetGoal();
							continue;
						}
					}

					// Default go to position.
					unit.GotoForced(World.ToWorldSpace(pointedAtPosition));						
					unit.ResetGoal();
				}
			}
			

			// Cleanup
			isInUnitSelectiodMode = false;
			pointedAtResource = null;
			selectedUnits.Clear();
			foreach(LineRenderer lineRenderer in lineRenderers)
			{
				lineRenderer.enabled = false;
			}
		}
	}

	//-------------------------------------------------
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
				teamId == hoveredUnit.factionId)
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

	BuildingData placementBuildingData;
	public void OnBuildingPlacementStarted(object sender, BuildMenuSlot.BuildingPlacementEvent e)
	{
		//SteamVR_Actions.construction.Activate();
		SetSnapTurnEnabled(false, false);
		isInBuildingPlacementMode = true;

		placementBuildingData = e.buildingData;
		
		// Get the world prefab and instatiate it.
		buildingPlacementPreviewObject = Instantiate(placementBuildingData.worldPreviewPrefab, destinationReticleTransform);
		buildingPlacementPreviewObject.transform.Rotate(0, 0, lastBuildingRotation);
		buildingPlacementPreviewObject.transform.localPosition = Vector3.zero;
	}

	private bool CanInteract( Hand hand)
	{
		//!PlayerManager.instance.handBuildMenu.activeSelf
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


	//-------------------------------------------------
	private void ShowPointer()
	{
		if ( !visible )
		{
			pointedAtPointerInteractable = null;
			pointerShowStartTime = Time.time;
			visible = true;
			//pointerObject.SetActive( false );
			teleportArc.Show();

			foreach ( PointerInteractable interactObject in interactableObjects )
				interactObject.Highlight( false );

			startingFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
			movedFeetFarEnough = false;
		}

		pointerStartTransform = pointerAttachmentPoint.transform;

		if ( pointerHand.currentAttachedObject != null )
		{
			//allowTeleportWhileAttached = pointerHand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();
		}
	}

	public void HardSnapToGrid(Transform obj, BuildingData buildingData)
    {
        Vector3 pos = World.ToWorldSpace(obj.position);

        obj.position = World.ToTransformSpace(new Vector3(Mathf.RoundToInt(pos.x), obj.position.y, Mathf.RoundToInt(pos.z)));
        	
        Vector3 modPos = obj.position;
        if (buildingData.boundingDimensionX % 2 == 0)
            modPos.x = obj.position.x + World.GetUnit() * -0.5f;
        
        if (buildingData.boundingDimensionY % 2 == 0)
            modPos.z = obj.position.z + World.GetUnit() * -0.5f;

        obj.position = modPos;
    }
		
	//-------------------------------------------------
	private void PlayAudioClip( AudioSource source, AudioClip clip )
	{
		source.clip = clip;
		source.Play();
	}


	//-------------------------------------------------
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
	
	//-------------------------------------------------
	private bool ShouldOverrideHoverLock()
	{
		if ( !allowTeleportWhileAttached || allowTeleportWhileAttached.overrideHoverLock )
			return true;

		return false;
	}

	//-------------------------------------------------
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
