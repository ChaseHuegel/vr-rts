
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Valve.VR.InteractionSystem;
using Valve.VR;

public class ObjectPlacementPointer : MonoBehaviour
{
	public SteamVR_Action_Boolean placeBuildingAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("BuildingPlacementPointer");

	public LayerMask traceLayerMask;
	public LayerMask allowedPlacementLayers;

	public Material pointVisibleMaterial;
	public Transform destinationReticleTransform;
	public Transform invalidReticleTransform;
	public Color pointerValidColor;
	public Color pointerInvalidColor;
	public float meshFadeTime = 0.2f;
	public float arcDistance = 10.0f;

	[Header( "Audio Sources" )]
	public AudioSource pointerAudioSource;
	public AudioSource loopingAudioSource;
	public AudioSource reticleAudioSource;

	[Header( "Sounds" )]
	public AudioClip pointerStartSound;
	public AudioClip pointerLoopSound;
	public AudioClip pointerStopSound;
	public AudioClip goodHighlightSound;
	public AudioClip badHighlightSound;
	private LineRenderer pointerLineRenderer;
	private GameObject interactionPointerObject;
	private Transform pointerStartTransform;
	private Hand pointerHand = null;
	private Player player = null;
	private TeleportArc teleportArc = null;

	public bool visible = false;

	private TerrainBuilding pointedAtTerrainBuilding;
	private Vector3 pointedAtPosition;
	private Vector3 prevPointedAtPosition;
	private float meshAlphaPercent = 1.0f;
	private float pointerShowStartTime = 0.0f;
	private float pointerHideStartTime = 0.0f;
	private bool meshFading = false;
	private float fullTintAlpha;

	private float invalidReticleMinScale = 0.2f;
	private float invalidReticleMaxScale = 1.0f;
	private float loopingAudioMaxVolume = 0.0f;
	private Coroutine hintCoroutine = null;
	private AllowTeleportWhileAttachedToHand allowTeleportWhileAttached = null;
	private Vector3 startingFeetOffset = Vector3.zero;
	private bool movedFeetFarEnough = false;

	//-------------------------------------------------
	private static ObjectPlacementPointer _instance;
	public static ObjectPlacementPointer instance
	{
		get
		{
			if ( _instance == null )
			{
				_instance = GameObject.FindObjectOfType<ObjectPlacementPointer>();
			}

			return _instance;
		}
	}


	//-------------------------------------------------
	void Awake()
	{
		_instance = this;

		pointerLineRenderer = GetComponentInChildren<LineRenderer>();
		interactionPointerObject = pointerLineRenderer.gameObject;

#if UNITY_URP
		fullTintAlpha = 0.5f;
#else
		int tintColorID = Shader.PropertyToID("_TintColor");
		fullTintAlpha = pointVisibleMaterial.GetColor(tintColorID).a;
#endif

		teleportArc = GetComponent<TeleportArc>();
		teleportArc.traceLayerMask = traceLayerMask;

		loopingAudioMaxVolume = loopingAudioSource.volume;

		float invalidReticleStartingScale = invalidReticleTransform.localScale.x;
		invalidReticleMinScale *= invalidReticleStartingScale;
		invalidReticleMaxScale *= invalidReticleStartingScale;
	}


	//-------------------------------------------------
	void Start()
	{
		player = Valve.VR.InteractionSystem.Player.instance;

		ShowPointer(player.rightHand, null);

		if ( player == null )
		{
			Debug.LogError("<b>[SteamVR Interaction]</b> ObjectPlacementPointer: No Player instance found in map.", this);
			Destroy( this.gameObject );
			return;
		}

		Invoke( "ShowTeleportHint", 5.0f );
	}

	//-------------------------------------------------
	void OnDisable()
	{
		HidePointer();
	}

	//-------------------------------------------------
	public void HideInteractionPointer()
	{
		if ( pointerHand != null )
		{
			HidePointer();
		}
	}

	public bool placementStarted;
	public Hand placementHand;
	public bool placementEnded;

	public void StartPlacement(Hand hand)
	{
		placementEnded = false;
		placementStarted = true;
		placementHand = hand;
	}

	public void StopPlacement(Hand hand)
	{
		placementStarted = false;	
		placementEnded = true;
		placementHand = hand;				
		visible = false;	
		HidePointer();
	}

	//-------------------------------------------------
	void Update()
	{
		
		Hand oldPointerHand = pointerHand;
		Hand newPointerHand = null;

		newPointerHand = placementHand;
		ShowPointer( newPointerHand, oldPointerHand );
		UpdatePointer();

        if ( visible )
            if ( placementEnded )
                if ( pointerHand == placementHand ) //This is the pointer hand
                    TryTeleportPlayer();

        if (placementStarted)
            newPointerHand = placementHand;

        if ( !visible && newPointerHand != null )
            //Begin showing the pointer
            ShowPointer( newPointerHand, oldPointerHand );
        else if ( visible )
            if ( newPointerHand == null && placementEnded)// !IsTeleportButtonDown( pointerHand ) )
                //Hide the pointer
                HidePointer();
            else if ( newPointerHand != null )
                //Move the pointer to a new hand
                ShowPointer( newPointerHand, oldPointerHand );

		if ( visible )
		{
			UpdatePointer();

			if ( meshFading )
				UpdateTeleportColors();
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
		//bool showPlayAreaPreview = false;
		Vector3 playerFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;

		Vector3 arcVelocity = pointerDir * arcDistance;

		TerrainBuilding hitTerrainBuilding = null;

		//Check pointer angle
		float dotUp = Vector3.Dot( pointerDir, Vector3.up );
		float dotForward = Vector3.Dot( pointerDir, player.hmdTransform.forward );
		bool pointerAtBadAngle = false;
		if ( ( dotForward > 0 && dotUp > 0.75f ) || ( dotForward < 0.0f && dotUp > 0.5f ) )
		{
			pointerAtBadAngle = true;
		}

		//Trace to see if the pointer hit anything
		RaycastHit hitInfo;
		teleportArc.SetArcData( pointerStart, arcVelocity, true, pointerAtBadAngle );
		if ( teleportArc.DrawArc( out hitInfo ) )
		{
			hitSomething = true;
			hitPointValid = LayerMatchTest( allowedPlacementLayers, hitInfo.collider.gameObject );
			hitTerrainBuilding = hitInfo.collider.GetComponentInParent<TerrainBuilding>();
		}

		HighlightSelected( hitTerrainBuilding );
			
        if (hitPointValid)
        {	teleportArc.SetColor( pointerValidColor );
#if (UNITY_5_4)
            pointerLineRenderer.SetColors( pointerValidColor, pointerValidColor );
#else
            pointerLineRenderer.startColor = pointerValidColor;
            pointerLineRenderer.endColor = pointerValidColor;
#endif
            destinationReticleTransform.gameObject.SetActive( true); //hitTeleportMarker.showReticle );
        }
        else // Not valid
        {
            teleportArc.SetColor( pointerInvalidColor );
#if (UNITY_5_4)
            pointerLineRenderer.SetColors( pointerInvalidColor, pointerInvalidColor );
#else
            pointerLineRenderer.startColor = pointerInvalidColor;
            pointerLineRenderer.endColor = pointerInvalidColor;
#endif			
        }

        pointedAtPosition = hitInfo.point;
        pointerEnd = hitInfo.point;

        if ( hitSomething )
        {
            pointerEnd = hitInfo.point;
        }
        else
        {
            pointerEnd = teleportArc.GetArcPositionAtTime( teleportArc.arcDuration );
        }

		destinationReticleTransform.position = pointedAtPosition;
		reticleAudioSource.transform.position = pointedAtPosition;
		pointerLineRenderer.SetPosition( 0, pointerStart );
		pointerLineRenderer.SetPosition( 1, pointerEnd );
		destinationReticleTransform.gameObject.SetActive( true );
	}
	
	private static bool LayerMatchTest(LayerMask layerMask, GameObject obj)
	{
		return ( ( 1 << obj.layer ) & layerMask ) != 0;
	}

	private void HidePointer()
	{
		if ( visible )
		{
			pointerHideStartTime = Time.time;
		}

		visible = false;
		if ( pointerHand )
		{
			//Stop looping sound
			loopingAudioSource.Stop();
			PlayAudioClip( pointerAudioSource, pointerStopSound );
		}

		interactionPointerObject.SetActive( false );
		teleportArc.Hide();
		pointerHand = null;
	}


	//-------------------------------------------------
	private void ShowPointer( Hand newPointerHand, Hand oldPointerHand )
	{
		if ( !visible )
		{
			pointerShowStartTime = Time.time;
			visible = true;
			meshFading = true;

			interactionPointerObject.SetActive( false );
			teleportArc.Show();

			startingFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
			movedFeetFarEnough = false;

			loopingAudioSource.clip = pointerLoopSound;
			loopingAudioSource.loop = true;
			loopingAudioSource.Play();
			loopingAudioSource.volume = 0.0f;
		}

		pointerHand = newPointerHand;

		if ( visible && oldPointerHand != pointerHand )
		{
			PlayAudioClip( pointerAudioSource, pointerStartSound );
		}

		if ( pointerHand )
		{
			pointerStartTransform = GetPointerStartTransform( pointerHand );

			pointerAudioSource.transform.SetParent( pointerStartTransform );
			pointerAudioSource.transform.localPosition = Vector3.zero;

			loopingAudioSource.transform.SetParent( pointerStartTransform );
			loopingAudioSource.transform.localPosition = Vector3.zero;
        }
	}


	//-------------------------------------------------
	private void UpdateTeleportColors()
	{
		float deltaTime = Time.time - pointerShowStartTime;
		if ( deltaTime > meshFadeTime )
		{
			meshAlphaPercent = 1.0f;
			meshFading = false;
		}
		else
		{
			meshAlphaPercent = Mathf.Lerp( 0.0f, 1.0f, deltaTime / meshFadeTime );
		}
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
		{
			if ( validLocation )
			{
				pointerHand.TriggerHapticPulse( 800 );
			}
			else
			{
				pointerHand.TriggerHapticPulse( 100 );
			}
		}
	}


	//-------------------------------------------------
	private void TryTeleportPlayer()
	{
		
	}


	//-------------------------------------------------
	private void InitiateTeleportFade()
	{
		
	}


	//-------------------------------------------------
	private void TeleportPlayer()
	{
		
	}


private void HighlightSelected( TerrainBuilding hitTerrainBuilding )
{
	if ( pointedAtTerrainBuilding != hitTerrainBuilding ) //Pointing at a new teleport marker
	{
		if ( hitTerrainBuilding != null )
		{
			prevPointedAtPosition = pointedAtPosition;
			PlayPointerHaptic( true );//!hitTerrainBuilding.locked );

			PlayAudioClip( reticleAudioSource, goodHighlightSound );

			loopingAudioSource.volume = loopingAudioMaxVolume;
		}
		else if ( pointedAtTerrainBuilding != null )
		{
			PlayAudioClip( reticleAudioSource, badHighlightSound );

			loopingAudioSource.volume = 0.0f;
		}
	}
	else if ( hitTerrainBuilding != null ) //Pointing at the same teleport marker
	{
		if ( Vector3.Distance( prevPointedAtPosition, pointedAtPosition ) > 1.0f )
		{
			prevPointedAtPosition = pointedAtPosition;
			PlayPointerHaptic( true ); //!hitTerrainBuilding.locked );
		}
	}
}
	//-------------------------------------------------
	public void ShowTeleportHint()
	{
		CancelTeleportHint();

		hintCoroutine = StartCoroutine( TeleportHintCoroutine() );
	}


	//-------------------------------------------------
	public void CancelTeleportHint()
	{
		if ( hintCoroutine != null )
		{
			ControllerButtonHints.HideTextHint(player.leftHand, placeBuildingAction);
			ControllerButtonHints.HideTextHint(player.rightHand, placeBuildingAction);

			StopCoroutine( hintCoroutine );
			hintCoroutine = null;
		}

		CancelInvoke( "ShowTeleportHint" );
	}


	//-------------------------------------------------
	private IEnumerator TeleportHintCoroutine()
	{
		float prevBreakTime = Time.time;
		float prevHapticPulseTime = Time.time;

		while ( true )
		{
			bool pulsed = false;

			//Show the hint on each eligible hand
			foreach ( Hand hand in player.hands )
			{
				bool showHint = IsEligibleForTeleport( hand );
				bool isShowingHint = !string.IsNullOrEmpty( ControllerButtonHints.GetActiveHintText( hand, placeBuildingAction) );
				if ( showHint )
				{
					if ( !isShowingHint )
					{
						ControllerButtonHints.ShowTextHint( hand, placeBuildingAction, "Teleport" );
						prevBreakTime = Time.time;
						prevHapticPulseTime = Time.time;
					}

					if ( Time.time > prevHapticPulseTime + 0.05f )
					{
						//Haptic pulse for a few seconds
						pulsed = true;

						hand.TriggerHapticPulse( 500 );
					}
				}
				else if ( !showHint && isShowingHint )
				{
					ControllerButtonHints.HideTextHint( hand, placeBuildingAction);
				}
			}

			if ( Time.time > prevBreakTime + 3.0f )
			{
				//Take a break for a few seconds
				yield return new WaitForSeconds( 3.0f );

				prevBreakTime = Time.time;
			}

			if ( pulsed )
			{
				prevHapticPulseTime = Time.time;
			}

			yield return null;
		}
	}


	//-------------------------------------------------
	public bool IsEligibleForTeleport( Hand hand )
	{
		if ( hand == null )
		{
			return false;
		}

		if ( !hand.gameObject.activeInHierarchy )
		{
			return false;
		}

		if ( hand.hoveringInteractable != null )
		{
			return false;
		}

		if ( hand.noSteamVRFallbackCamera == null )
		{
			if ( hand.isActive == false)
			{
				return false;
			}

			//Something is attached to the hand
			if ( hand.currentAttachedObject != null )
			{
				AllowTeleportWhileAttachedToHand allowTeleportWhileAttachedToHand = hand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();

				if ( allowTeleportWhileAttachedToHand != null && allowTeleportWhileAttachedToHand.teleportAllowed == true )
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		return true;
	}


	//-------------------------------------------------
	private bool ShouldOverrideHoverLock()
	{
		if ( !allowTeleportWhileAttached || allowTeleportWhileAttached.overrideHoverLock )
		{
			return true;
		}

		return false;
	}


	//-------------------------------------------------
	private bool WasTeleportButtonReleased( Hand hand )
	{
		if ( IsEligibleForTeleport( hand ) )
		{
			if ( hand.noSteamVRFallbackCamera != null )
			{
				return Input.GetKeyUp( KeyCode.T );
			}
			else
			{
				return placeBuildingAction.GetStateUp(hand.handType);

				//return hand.controller.GetPressUp( SteamVR_Controller.ButtonMask.Touchpad );
			}
		}

		return false;
	}

	//-------------------------------------------------
	private bool IsTeleportButtonDown( Hand hand )
	{
		if ( IsEligibleForTeleport( hand ) )
		{
			if ( hand.noSteamVRFallbackCamera != null )
			{
				return Input.GetKey( KeyCode.T );
			}
			else
			{
				return placeBuildingAction.GetState(hand.handType);
			}
		}

		return false;
	}


	//-------------------------------------------------
	private bool WasTeleportButtonPressed( Hand hand )
	{
		if ( IsEligibleForTeleport( hand ) )
		{
			if ( hand.noSteamVRFallbackCamera != null )
			{
				return Input.GetKeyDown( KeyCode.T );
			}
			else
			{
				return placeBuildingAction.GetStateDown(hand.handType);

				//return hand.controller.GetPressDown( SteamVR_Controller.ButtonMask.Touchpad );
			}
		}

		return false;
	}


	//-------------------------------------------------
	private Transform GetPointerStartTransform( Hand hand )
	{
		if ( hand.noSteamVRFallbackCamera != null )
		{
			return hand.noSteamVRFallbackCamera.transform;
		}
		else
		{
			return hand.transform;
		}
	}
}

