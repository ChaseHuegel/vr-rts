﻿
// using UnityEngine;
// using UnityEngine.Events;
// using System.Collections;
// using Valve.VR.InteractionSystem;
// using Valve.VR;

// public class ObjectPlacementPointer : MonoBehaviour
// {
// 	public SteamVR_Action_Boolean placeBuildingAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("BuildingPlacementPointer");

// 	public LayerMask traceLayerMask;
// 	public LayerMask allowedPlacementLayers;

// 	public LayerMask floorFixupTraceLayerMask;
// 	public float floorFixupMaximumTraceDistance = 1.0f;
// 	public Material areaVisibleMaterial;
// 	public Material areaLockedMaterial;
// 	public Material areaHighlightedMaterial;
// 	public Material pointVisibleMaterial;
// 	public Material pointLockedMaterial;
// 	public Material pointHighlightedMaterial;
// 	public Transform destinationReticleTransform;
// 	public Transform invalidReticleTransform;
// 	public GameObject playAreaPreviewCorner;
// 	public GameObject playAreaPreviewSide;
// 	public Color pointerValidColor;
// 	public Color pointerInvalidColor;
// 	public Color pointerLockedColor;
// 	public bool showPlayAreaMarker = true;

// 	public float teleportFadeTime = 0.1f;
// 	public float meshFadeTime = 0.2f;

// 	public float arcDistance = 10.0f;

// 	[Header( "Effects" )]
// 	public Transform onActivateObjectTransform;
// 	public Transform onDeactivateObjectTransform;
// 	public float activateObjectTime = 1.0f;
// 	public float deactivateObjectTime = 1.0f;

// 	[Header( "Audio Sources" )]
// 	public AudioSource pointerAudioSource;
// 	public AudioSource loopingAudioSource;
// 	public AudioSource headAudioSource;
// 	public AudioSource reticleAudioSource;

// 	[Header( "Sounds" )]
// 	public AudioClip teleportSound;
// 	public AudioClip pointerStartSound;
// 	public AudioClip pointerLoopSound;
// 	public AudioClip pointerStopSound;
// 	public AudioClip goodHighlightSound;
// 	public AudioClip badHighlightSound;

// 	[Header( "Debug" )]
// 	public bool debugFloor = false;
// 	public bool showOffsetReticle = false;
// 	public Transform offsetReticleTransform;
// 	public MeshRenderer floorDebugSphere;
// 	public LineRenderer floorDebugLine;

// 	private LineRenderer pointerLineRenderer;
// 	private GameObject interactionPointerObject;
// 	private Transform pointerStartTransform;
// 	private Hand pointerHand = null;
// 	private Player player = null;
// 	private TeleportArc teleportArc = null;

// 	public bool visible = false;

// 	private TeleportMarkerBase[] teleportMarkers;
// 	private TeleportMarkerBase pointedAtTeleportMarker;
// 	private TerrainBuilding pointedAtTerrainBuilding;
// 	private TeleportMarkerBase teleportingToMarker;
// 	private Vector3 pointedAtPosition;
// 	private Vector3 prevPointedAtPosition;
// 	private bool teleporting = false;
// 	private float currentFadeTime = 0.0f;

// 	private float meshAlphaPercent = 1.0f;
// 	private float pointerShowStartTime = 0.0f;
// 	private float pointerHideStartTime = 0.0f;
// 	private bool meshFading = false;
// 	private float fullTintAlpha;

// 	private float invalidReticleMinScale = 0.2f;
// 	private float invalidReticleMaxScale = 1.0f;
// 	private float invalidReticleMinScaleDistance = 0.4f;
// 	private float invalidReticleMaxScaleDistance = 2.0f;
// 	private Vector3 invalidReticleScale = Vector3.one;
// 	private Quaternion invalidReticleTargetRotation = Quaternion.identity;

// 	private Transform playAreaPreviewTransform;
// 	private Transform[] playAreaPreviewCorners;
// 	private Transform[] playAreaPreviewSides;

// 	private float loopingAudioMaxVolume = 0.0f;

// 	private Coroutine hintCoroutine = null;

// 	private bool originalHoverLockState = false;
// 	private Interactable originalHoveringInteractable = null;
// 	private AllowTeleportWhileAttachedToHand allowTeleportWhileAttached = null;

// 	private Vector3 startingFeetOffset = Vector3.zero;
// 	private bool movedFeetFarEnough = false;

// 	SteamVR_Events.Action chaperoneInfoInitializedAction;

// 	// Events

// 	public static SteamVR_Events.Event< float > ChangeScene = new SteamVR_Events.Event< float >();
// 	public static SteamVR_Events.Action< float > ChangeSceneAction( UnityAction< float > action ) { return new SteamVR_Events.Action< float >( ChangeScene, action ); }

// 	public static SteamVR_Events.Event< TeleportMarkerBase > Player = new SteamVR_Events.Event< TeleportMarkerBase >();
// 	public static SteamVR_Events.Action< TeleportMarkerBase > PlayerAction( UnityAction< TeleportMarkerBase > action ) { return new SteamVR_Events.Action< TeleportMarkerBase >( Player, action ); }

// 	public static SteamVR_Events.Event< TeleportMarkerBase > PlayerPre = new SteamVR_Events.Event< TeleportMarkerBase >();
// 	public static SteamVR_Events.Action< TeleportMarkerBase > PlayerPreAction( UnityAction< TeleportMarkerBase > action ) { return new SteamVR_Events.Action< TeleportMarkerBase >( PlayerPre, action ); }

// 	//-------------------------------------------------
// 	private static ObjectPlacementPointer _instance;
// 	public static ObjectPlacementPointer instance
// 	{
// 		get
// 		{
// 			if ( _instance == null )
// 			{
// 				_instance = GameObject.FindObjectOfType<ObjectPlacementPointer>();
// 			}

// 			return _instance;
// 		}
// 	}


// 	//-------------------------------------------------
// 	void Awake()
// 	{
// 		_instance = this;

// 		pointerLineRenderer = GetComponentInChildren<LineRenderer>();
// 		interactionPointerObject = pointerLineRenderer.gameObject;

// #if UNITY_URP
// 		fullTintAlpha = 0.5f;
// #else
// 		int tintColorID = Shader.PropertyToID("_TintColor");
// 		fullTintAlpha = pointVisibleMaterial.GetColor(tintColorID).a;
// #endif

// 		teleportArc = GetComponent<TeleportArc>();
// 		teleportArc.traceLayerMask = traceLayerMask;

// 		loopingAudioMaxVolume = loopingAudioSource.volume;

// 		float invalidReticleStartingScale = invalidReticleTransform.localScale.x;
// 		invalidReticleMinScale *= invalidReticleStartingScale;
// 		invalidReticleMaxScale *= invalidReticleStartingScale;
// 	}


// 	//-------------------------------------------------
// 	void Start()
// 	{
// 		//teleportMarkers = GameObject.FindObjectsOfType<TeleportMarkerBase>();

// 		// HidePointer();
// 		player = Valve.VR.InteractionSystem.Player.instance;

// 		ShowPointer(player.rightHand, null);

// 		if ( player == null )
// 		{
// 			Debug.LogError("<b>[SteamVR Interaction]</b> ObjectPlacementPointer: No Player instance found in map.", this);
// 			Destroy( this.gameObject );
// 			return;
// 		}

// 		Invoke( "ShowTeleportHint", 5.0f );
// 	}

// 	//-------------------------------------------------
// 	void OnDisable()
// 	{
// 		HidePointer();
// 	}

// 	//-------------------------------------------------
// 	public void HideInteractionPointer()
// 	{
// 		if ( pointerHand != null )
// 		{
// 			HidePointer();
// 		}
// 	}

// 	public bool placementStarted;
// 	public Hand placementHand;
// 	public bool placementEnded;

// 	public void StartPlacement(Hand hand)
// 	{
// 		placementEnded = false;
// 		placementStarted = true;
// 		placementHand = hand;
// 	}

// 	public void StopPlacement(Hand hand)
// 	{
// 		placementStarted = false;	
// 		placementEnded = true;
// 		placementHand = hand;				
// 		visible = false;	
// 		HidePointer();
// 	}

// 	//-------------------------------------------------
// 	void Update()
// 	{
		
// 		Hand oldPointerHand = pointerHand;
// 		Hand newPointerHand = null;

// 		newPointerHand = placementHand;
// 		ShowPointer( newPointerHand, oldPointerHand );
// 		UpdatePointer();

// 		return;

// 		// foreach ( Hand hand in player.hands )
// 		// {
// 			if ( visible )
// 			{	
// 				if ( placementEnded )
// 				//if ( WasTeleportButtonReleased( placementHand ) )
// 				{
// 					if ( pointerHand == placementHand ) //This is the pointer hand
// 					{
// 						TryTeleportPlayer();
// 					}
// 				}
// 			}

// 			if (placementStarted)
// 			//if ( WasTeleportButtonPressed( placementHand ) )
// 			{
// 				newPointerHand = placementHand;
// 			}
// 		// }

// 		//If something is attached to the hand that is preventing objectPlacement
// 		// if (false)// allowTeleportWhileAttached && !allowTeleportWhileAttached.teleportAllowed )
// 		// {
// 		// 	HidePointer();
// 		// }
// 		// else
// 		// {
// 			if ( !visible && newPointerHand != null )
// 			{
// 				//Begin showing the pointer
// 				ShowPointer( newPointerHand, oldPointerHand );
// 			}
// 			else if ( visible )
// 			{
// 				if ( newPointerHand == null && placementEnded)// !IsTeleportButtonDown( pointerHand ) )
// 				{
// 					//Hide the pointer
// 					HidePointer();
// 				}
// 				else if ( newPointerHand != null )
// 				{
// 					//Move the pointer to a new hand
// 					ShowPointer( newPointerHand, oldPointerHand );
// 				}
// 			}
// 		// }

// 		if ( visible )
// 		{
// 			UpdatePointer();

// 			if ( meshFading )
// 			{
// 				UpdateTeleportColors();
// 			}

// 			// if ( onActivateObjectTransform.gameObject.activeSelf && Time.time - pointerShowStartTime > activateObjectTime )
// 			// {
// 			// 	onActivateObjectTransform.gameObject.SetActive( false );
// 			// }
// 		}
// 		else
// 		{
// 			// if ( onDeactivateObjectTransform.gameObject.activeSelf && Time.time - pointerHideStartTime > deactivateObjectTime )
// 			// {
// 			// 	onDeactivateObjectTransform.gameObject.SetActive( false );
// 			// }
// 		}
// 	}


// 	//-------------------------------------------------
// 	private void UpdatePointer()
// 	{
// 		Vector3 pointerStart = pointerStartTransform.position;
// 		Vector3 pointerEnd;
// 		Vector3 pointerDir = pointerStartTransform.forward;
// 		bool hitSomething = false;
// 		bool hitPointValid = false;
// 		//bool showPlayAreaPreview = false;
// 		Vector3 playerFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;

// 		Vector3 arcVelocity = pointerDir * arcDistance;

// 		TeleportMarkerBase hitTeleportMarker = null;
// 		TerrainBuilding hitTerrainBuilding = null;

// 		//Check pointer angle
// 		float dotUp = Vector3.Dot( pointerDir, Vector3.up );
// 		float dotForward = Vector3.Dot( pointerDir, player.hmdTransform.forward );
// 		bool pointerAtBadAngle = false;
// 		if ( ( dotForward > 0 && dotUp > 0.75f ) || ( dotForward < 0.0f && dotUp > 0.5f ) )
// 		{
// 			pointerAtBadAngle = true;
// 		}

// 		//Trace to see if the pointer hit anything
// 		RaycastHit hitInfo;
// 		teleportArc.SetArcData( pointerStart, arcVelocity, true, pointerAtBadAngle );
// 		if ( teleportArc.DrawArc( out hitInfo ) )
// 		{
// 			hitSomething = true;
// 			//hitTeleportMarker = hitInfo.collider.GetComponentInParent<TeleportMarkerBase>();
		
// 			hitPointValid = LayerMatchTest( allowedPlacementLayers, hitInfo.collider.gameObject );
			
// 			hitTerrainBuilding = hitInfo.collider.GetComponentInParent<TerrainBuilding>();
// 			// Debug.Log(hitInfo.collider.gameObject.name);
// 		}
// //-------------------------------------------/////////////////////////////////////////
// 		// if ( pointerAtBadAngle )
// 		// {
// 		// 	hitTeleportMarker = null;
// 		// }

// 		//HighlightSelected( hitTeleportMarker );
// 		HighlightSelected( hitTerrainBuilding );

		
// 		// if ( hitTeleportMarker != null ) //Hit a teleport marker
// 		// {
// // 				if ( hitTeleportMarker.locked )
// // 				{
// // 					teleportArc.SetColor( pointerLockedColor );
// // #if (UNITY_5_4)
// // 					pointerLineRenderer.SetColors( pointerLockedColor, pointerLockedColor );
// // #else
// // 					pointerLineRenderer.startColor = pointerLockedColor;
// // 					pointerLineRenderer.endColor = pointerLockedColor;
// // #endif
// // 					destinationReticleTransform.gameObject.SetActive( false );
// // 				}
// // 				else
// // 				{
			
// 			if (hitPointValid)
// 			{	teleportArc.SetColor( pointerValidColor );
// #if (UNITY_5_4)
// 				pointerLineRenderer.SetColors( pointerValidColor, pointerValidColor );
// #else
// 				pointerLineRenderer.startColor = pointerValidColor;
// 				pointerLineRenderer.endColor = pointerValidColor;
// #endif
// 				destinationReticleTransform.gameObject.SetActive( true); //hitTeleportMarker.showReticle );
// 			}
// 			else // Not valid
// 			{
// 				// destinationReticleTransform.gameObject.SetActive( false );
// 				// offsetReticleTransform.gameObject.SetActive( false );
// 				teleportArc.SetColor( pointerInvalidColor );
// #if (UNITY_5_4)
// 				pointerLineRenderer.SetColors( pointerInvalidColor, pointerInvalidColor );
// #else
// 				pointerLineRenderer.startColor = pointerInvalidColor;
// 				pointerLineRenderer.endColor = pointerInvalidColor;
// #endif			
// 			}
// 			pointedAtPosition = hitInfo.point;
// 			pointerEnd = hitInfo.point;

// // 				invalidReticleTransform.gameObject.SetActive( !pointerAtBadAngle );

// // 				//Orient the invalid reticle to the normal of the trace hit point
// // 				Vector3 normalToUse = hitInfo.normal;
// // 				float angle = Vector3.Angle( hitInfo.normal, Vector3.up );
// // 				if ( angle < 15.0f )
// // 				{
// // 					normalToUse = Vector3.up;
// // 				}
// // 				invalidReticleTargetRotation = Quaternion.FromToRotation( Vector3.up, normalToUse );
// // 				invalidReticleTransform.rotation = Quaternion.Slerp( invalidReticleTransform.rotation, invalidReticleTargetRotation, 0.1f );

// // 				//Scale the invalid reticle based on the distance from the player
// // 				float distanceFromPlayer = Vector3.Distance( hitInfo.point, player.hmdTransform.position );
// // 				float invalidReticleCurrentScale = Util.RemapNumberClamped( distanceFromPlayer, invalidReticleMinScaleDistance, invalidReticleMaxScaleDistance, invalidReticleMinScale, invalidReticleMaxScale );
// // 				invalidReticleScale.x = invalidReticleCurrentScale;
// // 				invalidReticleScale.y = invalidReticleCurrentScale;
// // 				invalidReticleScale.z = invalidReticleCurrentScale;
// // 				invalidReticleTransform.transform.localScale = invalidReticleScale;

// // 				pointedAtTeleportMarker = null;
// 				//pointedAtTerrainBuilding = null;

// 			if ( hitSomething )
// 			{
// 				pointerEnd = hitInfo.point;
// 			}
// 			else
// 			{
// 				pointerEnd = teleportArc.GetArcPositionAtTime( teleportArc.arcDuration );
// 			}

// // 				//Debug floor
// // 				if ( debugFloor )
// // 				{
// // 					floorDebugSphere.gameObject.SetActive( false );
// // 					floorDebugLine.gameObject.SetActive( false );
// // 				}
// // 			}

// 		// if ( playAreaPreviewTransform != null )
// 		// {
// 		// 	playAreaPreviewTransform.gameObject.SetActive( showPlayAreaPreview );
// 		// }

// 		// if ( !showOffsetReticle )
// 		// {
// 		// 	offsetReticleTransform.gameObject.SetActive( false );
// 		// }

// 		destinationReticleTransform.position = pointedAtPosition;
		
// 		// invalidReticleTransform.position = pointerEnd;
// 		// onActivateObjectTransform.position = pointerEnd;
// 		// onDeactivateObjectTransform.position = pointerEnd;
// 		// offsetReticleTransform.position = pointerEnd - playerFeetOffset;

// 		reticleAudioSource.transform.position = pointedAtPosition;

// 		pointerLineRenderer.SetPosition( 0, pointerStart );
// 		pointerLineRenderer.SetPosition( 1, pointerEnd );
		
// 		// Added this.
// 		destinationReticleTransform.gameObject.SetActive( true );

// 	}
	
// 	private static bool LayerMatchTest(LayerMask layerMask, GameObject obj)
// 	{
// 		return ( ( 1 << obj.layer ) & layerMask ) != 0;
// 	}

// 	//-------------------------------------------------
// 	void FixedUpdate()
// 	{
// 		if ( !visible )
// 		{
// 			return;
// 		}			
// 	}

// 	private void HidePointer()
// 	{
// 		if ( visible )
// 		{
// 			pointerHideStartTime = Time.time;
// 		}

// 		visible = false;
// 		if ( pointerHand )
// 		{
// 			// if ( ShouldOverrideHoverLock() )
// 			// {
// 			// 	//Restore the original hovering interactable on the hand
// 			// 	if ( originalHoverLockState == true )
// 			// 	{
// 			// 		pointerHand.HoverLock( originalHoveringInteractable );
// 			// 	}
// 			// 	else
// 			// 	{
// 			// 		pointerHand.HoverUnlock( null );
// 			// 	}
// 			// }

// 			//Stop looping sound
// 			loopingAudioSource.Stop();
// 			PlayAudioClip( pointerAudioSource, pointerStopSound );
// 		}
// 		interactionPointerObject.SetActive( false );

// 		teleportArc.Hide();

// 		// foreach ( TeleportMarkerBase teleportMarker in teleportMarkers )
// 		// {
// 		// 	if ( teleportMarker != null && teleportMarker.markerActive && teleportMarker.gameObject != null )
// 		// 	{
// 		// 		teleportMarker.gameObject.SetActive( false );
// 		// 	}
// 		// }

// 		// destinationReticleTransform.gameObject.SetActive( false );
// 		// invalidReticleTransform.gameObject.SetActive( false );
// 		// offsetReticleTransform.gameObject.SetActive( false );

// 		// if ( playAreaPreviewTransform != null )
// 		// {
// 		// 	playAreaPreviewTransform.gameObject.SetActive( false );
// 		// }

// 		// if ( onActivateObjectTransform.gameObject.activeSelf )
// 		// {
// 		// 	onActivateObjectTransform.gameObject.SetActive( false );
// 		// }
// 		// onDeactivateObjectTransform.gameObject.SetActive( true );

// 		pointerHand = null;
// 	}


// 	//-------------------------------------------------
// 	private void ShowPointer( Hand newPointerHand, Hand oldPointerHand )
// 	{
// 		if ( !visible )
// 		{
// 			//pointedAtTeleportMarker = null;
// 			pointerShowStartTime = Time.time;
// 			visible = true;
// 			meshFading = true;

// 			interactionPointerObject.SetActive( false );
// 			teleportArc.Show();

// 			// foreach ( TeleportMarkerBase teleportMarker in teleportMarkers )
// 			// {
// 			// 	if ( teleportMarker.markerActive && teleportMarker.ShouldActivate( player.feetPositionGuess ) )
// 			// 	{
// 			// 		teleportMarker.gameObject.SetActive( true );
// 			// 		teleportMarker.Highlight( false );
// 			// 	}
// 			// }

// 			startingFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
// 			movedFeetFarEnough = false;

// 			// if ( onDeactivateObjectTransform.gameObject.activeSelf )
// 			// {
// 			// 	onDeactivateObjectTransform.gameObject.SetActive( false );
// 			// }
// 			// onActivateObjectTransform.gameObject.SetActive( true );

// 			loopingAudioSource.clip = pointerLoopSound;
// 			loopingAudioSource.loop = true;
// 			loopingAudioSource.Play();
// 			loopingAudioSource.volume = 0.0f;
// 		}


// 		// if ( oldPointerHand )
// 		// {
// 		// 	if ( ShouldOverrideHoverLock() )
// 		// 	{
// 		// 		//Restore the original hovering interactable on the hand
// 		// 		if ( originalHoverLockState == true )
// 		// 		{
// 		// 			oldPointerHand.HoverLock( originalHoveringInteractable );
// 		// 		}
// 		// 		else
// 		// 		{
// 		// 			oldPointerHand.HoverUnlock( null );
// 		// 		}
// 		// 	}
// 		// }

// 		pointerHand = newPointerHand;

// 		if ( visible && oldPointerHand != pointerHand )
// 		{
// 			PlayAudioClip( pointerAudioSource, pointerStartSound );
// 		}

// 		if ( pointerHand )
// 		{
// 			pointerStartTransform = GetPointerStartTransform( pointerHand );

// 			if ( pointerHand.currentAttachedObject != null )
// 			{
// 				//allowTeleportWhileAttached = pointerHand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();
// 			}

// 			//Keep track of any existing hovering interactable on the hand
// 			// originalHoverLockState = pointerHand.hoverLocked;
// 			// originalHoveringInteractable = pointerHand.hoveringInteractable;

// 			// if ( ShouldOverrideHoverLock() )
// 			// {
// 			// 	pointerHand.HoverLock( null );
// 			// }

// 			pointerAudioSource.transform.SetParent( pointerStartTransform );
// 			pointerAudioSource.transform.localPosition = Vector3.zero;

// 			loopingAudioSource.transform.SetParent( pointerStartTransform );
// 			loopingAudioSource.transform.localPosition = Vector3.zero;
// 			}
// 	}


// 	//-------------------------------------------------
// 	private void UpdateTeleportColors()
// 	{
// 		float deltaTime = Time.time - pointerShowStartTime;
// 		if ( deltaTime > meshFadeTime )
// 		{
// 			meshAlphaPercent = 1.0f;
// 			meshFading = false;
// 		}
// 		else
// 		{
// 			meshAlphaPercent = Mathf.Lerp( 0.0f, 1.0f, deltaTime / meshFadeTime );
// 		}

// 		//Tint color for the teleport points
// 		// foreach ( TeleportMarkerBase teleportMarker in teleportMarkers )
// 		// {
// 		// 	teleportMarker.SetAlpha( fullTintAlpha * meshAlphaPercent, meshAlphaPercent );
// 		// }
// 	}


// 	//-------------------------------------------------
// 	private void PlayAudioClip( AudioSource source, AudioClip clip )
// 	{
// 		source.clip = clip;
// 		source.Play();
// 	}


// 	//-------------------------------------------------
// 	private void PlayPointerHaptic( bool validLocation )
// 	{
// 		if ( pointerHand != null )
// 		{
// 			if ( validLocation )
// 			{
// 				pointerHand.TriggerHapticPulse( 800 );
// 			}
// 			else
// 			{
// 				pointerHand.TriggerHapticPulse( 100 );
// 			}
// 		}
// 	}


// 	//-------------------------------------------------
// 	private void TryTeleportPlayer()
// 	{
// 		// if ( visible)
// 		// {

// 		// }

// 		// if ( visible && !teleporting )
// 		// {
// 		// 	if ( pointedAtTeleportMarker != null && pointedAtTeleportMarker.locked == false )
// 		// 	{
// 		// 		//Pointing at an unlocked teleport marker
// 		// 		teleportingToMarker = pointedAtTeleportMarker;
// 		// 		InitiateTeleportFade();

// 		// 		CancelTeleportHint();
// 		// 	}
// 		// }
// 	}


// 	//-------------------------------------------------
// 	private void InitiateTeleportFade()
// 	{
// 		// teleporting = true;

// 		// currentFadeTime = teleportFadeTime;

// 		// TeleportPoint teleportPoint = teleportingToMarker as TeleportPoint;
// 		// if ( teleportPoint != null && teleportPoint.teleportType == TeleportPoint.TeleportPointType.SwitchToNewScene )
// 		// {
// 		// 	currentFadeTime *= 3.0f;
// 		// 	ObjectPlacementPointer.ChangeScene.Send( currentFadeTime );
// 		// }

// 		// SteamVR_Fade.Start( Color.clear, 0 );
// 		// SteamVR_Fade.Start( Color.black, currentFadeTime );

// 		// headAudioSource.transform.SetParent( player.hmdTransform );
// 		// headAudioSource.transform.localPosition = Vector3.zero;
// 		// PlayAudioClip( headAudioSource, teleportSound );

// 		// Invoke( "TeleportPlayer", currentFadeTime );
// 	}


// 	//-------------------------------------------------
// 	private void TeleportPlayer()
// 	{
// 		// teleporting = false;

// 		// ObjectPlacementPointer.PlayerPre.Send( pointedAtTeleportMarker );

// 		// SteamVR_Fade.Start( Color.clear, currentFadeTime );

// 		// TeleportPoint teleportPoint = teleportingToMarker as TeleportPoint;
// 		// Vector3 teleportPosition = pointedAtPosition;

// 		// if ( teleportPoint != null )
// 		// {
// 		// 	teleportPosition = teleportPoint.transform.position;

// 		// 	//Teleport to a new scene
// 		// 	if ( teleportPoint.teleportType == TeleportPoint.TeleportPointType.SwitchToNewScene )
// 		// 	{
// 		// 		teleportPoint.TeleportToScene();
// 		// 		return;
// 		// 	}
// 		// }

// 		// // Find the actual floor position below the navigation mesh
// 		// TeleportArea teleportArea = teleportingToMarker as TeleportArea;
// 		// if ( teleportArea != null )
// 		// {
// 		// 	if ( floorFixupMaximumTraceDistance > 0.0f )
// 		// 	{
// 		// 		RaycastHit raycastHit;
// 		// 		if ( Physics.Raycast( teleportPosition + 0.05f * Vector3.down, Vector3.down, out raycastHit, floorFixupMaximumTraceDistance, floorFixupTraceLayerMask ) )
// 		// 		{
// 		// 			teleportPosition = raycastHit.point;
// 		// 		}
// 		// 	}
// 		// }

// 		// if ( teleportingToMarker.ShouldMovePlayer() )
// 		// {
// 		// 	Vector3 playerFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
// 		// 	player.trackingOriginTransform.position = teleportPosition + playerFeetOffset;

// 		//     if (player.leftHand.currentAttachedObjectInfo.HasValue)
// 		//         player.leftHand.ResetAttachedTransform(player.leftHand.currentAttachedObjectInfo.Value);
// 		//     if (player.rightHand.currentAttachedObjectInfo.HasValue)
// 		//         player.rightHand.ResetAttachedTransform(player.rightHand.currentAttachedObjectInfo.Value);
// 		// }
// 		// else
// 		// {
// 		// 	teleportingToMarker.TeleportPlayer( pointedAtPosition );
// 		// }

// 		// ObjectPlacementPointer.Player.Send( pointedAtTeleportMarker );
// 	}


// private void HighlightSelected( TerrainBuilding hitTerrainBuilding )
// {
// 	if ( pointedAtTerrainBuilding != hitTerrainBuilding ) //Pointing at a new teleport marker
// 	{
// 		if ( pointedAtTerrainBuilding != null )
// 		{
// 			pointedAtTerrainBuilding.Highlight( false );
// 		}

// 		if ( hitTerrainBuilding != null )
// 		{
// 			hitTerrainBuilding.Highlight( true );

// 			prevPointedAtPosition = pointedAtPosition;
// 			PlayPointerHaptic( true );//!hitTerrainBuilding.locked );

// 			PlayAudioClip( reticleAudioSource, goodHighlightSound );

// 			loopingAudioSource.volume = loopingAudioMaxVolume;
// 		}
// 		else if ( pointedAtTerrainBuilding != null )
// 		{
// 			PlayAudioClip( reticleAudioSource, badHighlightSound );

// 			loopingAudioSource.volume = 0.0f;
// 		}
// 	}
// 	else if ( hitTerrainBuilding != null ) //Pointing at the same teleport marker
// 	{
// 		if ( Vector3.Distance( prevPointedAtPosition, pointedAtPosition ) > 1.0f )
// 		{
// 			prevPointedAtPosition = pointedAtPosition;
// 			PlayPointerHaptic( true ); //!hitTerrainBuilding.locked );
// 		}
// 	}
// }
// 	//-------------------------------------------------
// 	public void ShowTeleportHint()
// 	{
// 		CancelTeleportHint();

// 		hintCoroutine = StartCoroutine( TeleportHintCoroutine() );
// 	}


// 	//-------------------------------------------------
// 	public void CancelTeleportHint()
// 	{
// 		if ( hintCoroutine != null )
// 		{
// 			ControllerButtonHints.HideTextHint(player.leftHand, placeBuildingAction);
// 			ControllerButtonHints.HideTextHint(player.rightHand, placeBuildingAction);

// 			StopCoroutine( hintCoroutine );
// 			hintCoroutine = null;
// 		}

// 		CancelInvoke( "ShowTeleportHint" );
// 	}


// 	//-------------------------------------------------
// 	private IEnumerator TeleportHintCoroutine()
// 	{
// 		float prevBreakTime = Time.time;
// 		float prevHapticPulseTime = Time.time;

// 		while ( true )
// 		{
// 			bool pulsed = false;

// 			//Show the hint on each eligible hand
// 			foreach ( Hand hand in player.hands )
// 			{
// 				bool showHint = IsEligibleForTeleport( hand );
// 				bool isShowingHint = !string.IsNullOrEmpty( ControllerButtonHints.GetActiveHintText( hand, placeBuildingAction) );
// 				if ( showHint )
// 				{
// 					if ( !isShowingHint )
// 					{
// 						ControllerButtonHints.ShowTextHint( hand, placeBuildingAction, "Teleport" );
// 						prevBreakTime = Time.time;
// 						prevHapticPulseTime = Time.time;
// 					}

// 					if ( Time.time > prevHapticPulseTime + 0.05f )
// 					{
// 						//Haptic pulse for a few seconds
// 						pulsed = true;

// 						hand.TriggerHapticPulse( 500 );
// 					}
// 				}
// 				else if ( !showHint && isShowingHint )
// 				{
// 					ControllerButtonHints.HideTextHint( hand, placeBuildingAction);
// 				}
// 			}

// 			if ( Time.time > prevBreakTime + 3.0f )
// 			{
// 				//Take a break for a few seconds
// 				yield return new WaitForSeconds( 3.0f );

// 				prevBreakTime = Time.time;
// 			}

// 			if ( pulsed )
// 			{
// 				prevHapticPulseTime = Time.time;
// 			}

// 			yield return null;
// 		}
// 	}


// 	//-------------------------------------------------
// 	public bool IsEligibleForTeleport( Hand hand )
// 	{
// 		if ( hand == null )
// 		{
// 			return false;
// 		}

// 		if ( !hand.gameObject.activeInHierarchy )
// 		{
// 			return false;
// 		}

// 		if ( hand.hoveringInteractable != null )
// 		{
// 			return false;
// 		}

// 		if ( hand.noSteamVRFallbackCamera == null )
// 		{
// 			if ( hand.isActive == false)
// 			{
// 				return false;
// 			}

// 			//Something is attached to the hand
// 			if ( hand.currentAttachedObject != null )
// 			{
// 				AllowTeleportWhileAttachedToHand allowTeleportWhileAttachedToHand = hand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();

// 				if ( allowTeleportWhileAttachedToHand != null && allowTeleportWhileAttachedToHand.teleportAllowed == true )
// 				{
// 					return true;
// 				}
// 				else
// 				{
// 					return false;
// 				}
// 			}
// 		}

// 		return true;
// 	}


// 	//-------------------------------------------------
// 	private bool ShouldOverrideHoverLock()
// 	{
// 		if ( !allowTeleportWhileAttached || allowTeleportWhileAttached.overrideHoverLock )
// 		{
// 			return true;
// 		}

// 		return false;
// 	}


// 	//-------------------------------------------------
// 	private bool WasTeleportButtonReleased( Hand hand )
// 	{
// 		if ( IsEligibleForTeleport( hand ) )
// 		{
// 			if ( hand.noSteamVRFallbackCamera != null )
// 			{
// 				return Input.GetKeyUp( KeyCode.T );
// 			}
// 			else
// 			{
// 				return placeBuildingAction.GetStateUp(hand.handType);

// 				//return hand.controller.GetPressUp( SteamVR_Controller.ButtonMask.Touchpad );
// 			}
// 		}

// 		return false;
// 	}

// 	//-------------------------------------------------
// 	private bool IsTeleportButtonDown( Hand hand )
// 	{
// 		if ( IsEligibleForTeleport( hand ) )
// 		{
// 			if ( hand.noSteamVRFallbackCamera != null )
// 			{
// 				return Input.GetKey( KeyCode.T );
// 			}
// 			else
// 			{
// 				return placeBuildingAction.GetState(hand.handType);
// 			}
// 		}

// 		return false;
// 	}


// 	//-------------------------------------------------
// 	private bool WasTeleportButtonPressed( Hand hand )
// 	{
// 		if ( IsEligibleForTeleport( hand ) )
// 		{
// 			if ( hand.noSteamVRFallbackCamera != null )
// 			{
// 				return Input.GetKeyDown( KeyCode.T );
// 			}
// 			else
// 			{
// 				return placeBuildingAction.GetStateDown(hand.handType);

// 				//return hand.controller.GetPressDown( SteamVR_Controller.ButtonMask.Touchpad );
// 			}
// 		}

// 		return false;
// 	}


// 	//-------------------------------------------------
// 	private Transform GetPointerStartTransform( Hand hand )
// 	{
// 		if ( hand.noSteamVRFallbackCamera != null )
// 		{
// 			return hand.noSteamVRFallbackCamera.transform;
// 		}
// 		else
// 		{
// 			return hand.transform;
// 		}
// 	}
// }
