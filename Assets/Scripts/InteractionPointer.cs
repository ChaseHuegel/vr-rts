
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Valve.VR.InteractionSystem;
using Valve.VR;

public class InteractionPointer : MonoBehaviour
{
	
	public SteamVR_Action_Boolean placeBuildingAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("BuildingPlacementPointer");
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
	public AudioClip pointerLoopSound;
	public AudioClip pointerStopSound;
	public AudioClip goodHighlightSound;
	public AudioClip badHighlightSound;

	private LineRenderer pointerLineRenderer;
	private GameObject interactionPointerObject;
	private Transform pointerStartTransform;
	public Hand pointerHand = null;
	private Player player = null;
	private TeleportArc teleportArc = null;
	public bool visible = false;
	private PointerInteractable[] interactableObjects;
	private PointerInteractable pointedAtPointerInteractable;
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
		// pointerLineRenderer = GetComponentInChildren<LineRenderer>();
		// interactionPointerObject = pointerLineRenderer.gameObject;

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
		interactableObjects = GameObject.FindObjectsOfType<PointerInteractable>();
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
	void OnDisable()
	{
		HidePointer();
	}

	public bool placementStarted;
	public Hand placementHand;
	public bool placementEnded;

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
		// {
			UpdatePointer();
			
		// }
		// else
		// {
		// 	ShowPointer();
		// }
			
			
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
		if ( ( dotForward > 0 && dotUp > 0.75f ) || ( dotForward < 0.0f && dotUp > 0.5f ) )
		{
			pointerAtBadAngle = true;
		}

		//Trace to see if the pointer hit anything
		RaycastHit hitInfo;
		teleportArc.SetArcData( pointerStart, arcVelocity, true, pointerAtBadAngle );

		teleportArc.FindProjectileCollision( out hitInfo );
		//if ( teleportArc.DrawArc( out hitInfo ) )
		if ( hitInfo.collider )
		{	
			hitSomething = true;
			hitPointValid = LayerMatchTest( allowedPlacementLayers, hitInfo.collider.gameObject );
			hitPointerInteractable = hitInfo.collider.GetComponentInParent<PointerInteractable>();
		}
		
		HighlightSelected( hitPointerInteractable );

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
		//interactionPointerObject.SetActive( false );
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
			//interactionPointerObject.SetActive( false );
			teleportArc.Show();

			foreach ( PointerInteractable interactObject in interactableObjects )
			{
				interactObject.Highlight( false );
			}

			startingFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
			movedFeetFarEnough = false;
		}

		pointerStartTransform = pointerAttachmentPoint.transform;

		if ( pointerHand.currentAttachedObject != null )
		{
			//allowTeleportWhileAttached = pointerHand.currentAttachedObject.GetComponent<AllowTeleportWhileAttachedToHand>();
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


	private void HighlightSelected( PointerInteractable hitPointerInteractable )
	{
		if ( pointedAtPointerInteractable != hitPointerInteractable ) //Pointing at a new teleport marker
		{
			if ( pointedAtPointerInteractable != null )
			{
				pointedAtPointerInteractable.Highlight( false );
			}

			if ( hitPointerInteractable != null )
			{
				hitPointerInteractable.Highlight( true );

				prevPointedAtPosition = pointedAtPosition;
				PlayPointerHaptic( true );//!hitTerrainBuilding.locked );

				//PlayAudioClip( reticleAudioSource, goodHighlightSound );

				// loopingAudioSource.volume = loopingAudioMaxVolume;
			}
			else if ( pointedAtPointerInteractable != null )
			{
				//PlayAudioClip( reticleAudioSource, badHighlightSound );

				// loopingAudioSource.volume = 0.0f;
			}
		}
		else if ( hitPointerInteractable != null ) //Pointing at the same teleport marker
		{
			if ( Vector3.Distance( prevPointedAtPosition, pointedAtPosition ) > 1.0f )
			{
				prevPointedAtPosition = pointedAtPosition;
				PlayPointerHaptic( true ); //!hitTerrainBuilding.locked );
			}
		}
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

}

