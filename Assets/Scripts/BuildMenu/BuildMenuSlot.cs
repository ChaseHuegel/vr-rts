using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;
using Valve.VR;
using System;
using Swordfish;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent( typeof( Interactable ) )]
public class BuildMenuSlot : MonoBehaviour
{
    public SteamVR_Action_Boolean selectAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Select");
	public SteamVR_Action_Boolean cancelAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Cancel");

    public BuildingData buildingData;
    public bool requireGrabActionToTake = true;
    public bool requireReleaseActionToReturn = false;
    public bool showTriggerHint = false;

    [EnumFlags]
    public Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags;
    private GameObject spawnedItem;
    private bool itemIsSpawned = false;
    public UnityEvent pickupEvent;
    public UnityEvent dropEvent;    

    [SerializeField]
    private GameObject fadedPreviewObject;

    [SerializeField]
    private GameObject normalPreviewObject;
    public bool justPickedUpItem = false;
    public SphereCollider grabCollider;
    private Vector3 previewObjectOriginalScale;
    public GameObject lockObject;
    public GameObject iconObject;

    public delegate void OnHandHoverBeginEvent(TechBase techBase);
    public event OnHandHoverBeginEvent HandHoverBegin;

    public delegate void OnHandHoverEndEvent(TechBase techBase);
    public event OnHandHoverEndEvent HandHoverEnd;

    void Start()
    {
        Initialize();        
    }

    public void Initialize()
    {
        previewObjectOriginalScale = normalPreviewObject.transform.localScale;

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        spriteRenderer.sprite = Sprite.Create(buildingData.worldQueueTexture, new Rect(0f, 0f, buildingData.worldQueueTexture.width, buildingData.worldQueueTexture.height), new Vector2(0.5f, 0.5f), 100.0f, 1, SpriteMeshType.Tight, Vector4.zero, true);

        HookIntoEvents();
    }

    private void Lock()
    {
        iconObject.SetActive(true);
        lockObject.SetActive(true);
        normalPreviewObject.SetActive(false);
    }

    private void Unlock()
    {
        iconObject?.SetActive(false);
        lockObject?.SetActive(false);
        normalPreviewObject.SetActive(true);
    }

    private void Enable()
    {
        grabCollider.enabled = true;
        iconObject.SetActive(false);
        normalPreviewObject?.SetActive(true);
    }

    private void Disable()
    {
        grabCollider.enabled = false;
        iconObject.SetActive(true);
        normalPreviewObject?.SetActive(false);
    }

    private void OnNodeLocked(TechNode node)
    {
        if (node.tech == buildingData)
            Lock();
    }

    private void OnNodeUnlocked(TechNode node)
    {
        if (node.tech == buildingData)
            Unlock();
    }

    private void OnNodeEnabled(TechNode node)
    {
        if (node.tech == buildingData)
            Enable();
    }

    private void OnNodeDisabled(TechNode node)
    {   
        if (node.tech == buildingData)
            Disable();
    }

    private void HookIntoEvents()
    {
        TechTree.OnNodeUnlocked += OnNodeUnlocked;
        TechTree.OnNodeLocked += OnNodeLocked;
        TechTree.OnNodeEnabled += OnNodeEnabled;
        TechTree.OnNodeDisabled += OnNodeDisabled;
    }

    private void CleanupEvents()
    {
        TechTree.OnNodeUnlocked -= OnNodeUnlocked;
        TechTree.OnNodeLocked -= OnNodeLocked;
        TechTree.OnNodeEnabled -= OnNodeEnabled;
        TechTree.OnNodeDisabled -= OnNodeDisabled;
    }

    void OnDestroy()
    {
        CleanupEvents();
    }

    public void CreatePreviewObjects()
    {
        if (buildingData.menuPreviewPrefab != null && !normalPreviewObject)
        {
            normalPreviewObject = Instantiate( buildingData.menuPreviewPrefab, transform.position, Quaternion.identity ) as GameObject;
            normalPreviewObject.name = "_" + normalPreviewObject.name;
            normalPreviewObject.transform.parent = transform;
            normalPreviewObject.transform.localRotation = Quaternion.identity;
        }            
    
        if (buildingData.fadedPreviewPrefab != null && !fadedPreviewObject)
        {
            fadedPreviewObject = Instantiate( buildingData.fadedPreviewPrefab, transform.position, Quaternion.identity ) as GameObject;
            fadedPreviewObject.name = "_" + fadedPreviewObject.name;
            fadedPreviewObject.transform.parent = transform;
            fadedPreviewObject.transform.localRotation = Quaternion.identity;
            fadedPreviewObject.SetActive(false);
        }
    }

    void Update()
    {
        if ( ( itemIsSpawned == true ) && ( spawnedItem == null ) )
        {
            itemIsSpawned = false;
            dropEvent?.Invoke();           
        }                  
    }

    private void ActivateNormalPreviewObject()
    {
        fadedPreviewObject.SetActive(false);
        normalPreviewObject.SetActive(true);        
    }

    private void ActivateFadedPreviewObject()
    {
        normalPreviewObject.SetActive(false);
        fadedPreviewObject.SetActive(true);
    }

    void SwapPreviewObject()
    {
        if (normalPreviewObject.activeSelf)
            ActivateFadedPreviewObject();
        else
            ActivateNormalPreviewObject();
    }

    private void ScaleUp()
    {
        normalPreviewObject.transform.localScale = previewObjectOriginalScale * 1.25f;
    }
    
    private void ResetScale()
    {
        normalPreviewObject.transform.localScale = previewObjectOriginalScale;
    }

    private void OnHandHoverBegin( Hand hand )
    {
        ThrowableBuilding currentAttachedThrowableBuilding = GetAttachedThrowableBuilding( hand );
    
        if ( currentAttachedThrowableBuilding )
        {
            // If we want to take back the item and we aren't waiting for a trigger press
            if ( !requireReleaseActionToReturn && !justPickedUpItem)
            {
                if(buildingData.buildingType == currentAttachedThrowableBuilding.rtsBuildingTypeData.buildingType)
                    TakeBackItem( hand );
            }
        }

        ScaleUp();

        if (HandHoverBegin != null)
            HandHoverBegin.Invoke(buildingData);

        // We don't require trigger press for pickup. Spawn and attach object.
        if (!requireGrabActionToTake)
        {
            SpawnAndAttachObject( hand, GrabTypes.Scripted );
        }

        if (requireGrabActionToTake && showTriggerHint )
        {
            hand.ShowGrabHint("PickUp");
        }
    }

    private void HandHoverUpdate( Hand hand )
    {
        if ( requireReleaseActionToReturn  && !justPickedUpItem)
        {
            if (hand.isActive)
            {
                ThrowableBuilding currentAttachedThrowableBuilding = GetAttachedThrowableBuilding( hand );
                if (currentAttachedThrowableBuilding && hand.IsGrabEnding(currentAttachedThrowableBuilding.gameObject))
                {
                    TakeBackItem( hand );

                    // So that we don't pick up a throwable building the same frame that we return it
                    return;
                }
            }
        }

        if ( requireGrabActionToTake && !isPointerPlacementStarting)
        {
            GrabTypes startingGrab = hand.GetGrabStarting();

            if (startingGrab != GrabTypes.None)
            {
                if (startingGrab == GrabTypes.Trigger || startingGrab == GrabTypes.Pinch)
                    SpawnAndAttachObject(hand, startingGrab);// GrabTypes.Scripted);
            }
        }
        
        if (WasSelectButtonPressed(hand))
        {
            if (!isPointerPlacementStarting)
                isPointerPlacementStarting = true;
        }

        if (WasSelectButtonReleased(hand))
        {
            if (isPointerPlacementStarting)
            {                
                BuildingPlacementEvent e = new BuildingPlacementEvent{ buildingData = buildingData, hand = hand };
                OnBuildingPlacementEvent?.Invoke(this, e);
                // itemIsSpawned = true;
                // useFadedPreview = true;
                // CreatePreviewObject();

                isPointerPlacementStarting = false;
                if (e.cancel)
                    Debug.Log("event cancelled.");
            }
        }        
    }
    
    private bool isPointerPlacementStarting;

    private bool WasSelectButtonReleased(Hand hand)
    {
        return selectAction.GetStateUp(hand.handType);
    }

    private bool WasSelectButtonPressed(Hand hand)
    {
        return selectAction.GetStateDown(hand.handType);
    }

    //-------------------------------------------------
    private void OnHandHoverEnd( Hand hand )
    {
        if ( !justPickedUpItem && requireGrabActionToTake && showTriggerHint )
            hand.HideGrabHint();

        justPickedUpItem = false;
        ResetScale();

        if (HandHoverEnd != null)
            HandHoverEnd.Invoke(buildingData);
    }

    private ThrowableBuilding GetAttachedThrowableBuilding( Hand hand )
    {
        GameObject currentAttachedObject = hand.currentAttachedObject;

        // Verify the hand is holding something
        if ( currentAttachedObject == null )
            return null;

        ThrowableBuilding throwableBuilding = hand.currentAttachedObject.GetComponent<ThrowableBuilding>();

        // Verify the item in the hand is a throwable building
        if ( throwableBuilding == null )
            return null;

        return throwableBuilding;
    }

    private void TakeBackItem( Hand hand )
    {
        ActivateNormalPreviewObject();
        GameObject detachedItem = hand.AttachedObjects[0].attachedObject;
        hand.DetachObject( detachedItem );
        Destroy(detachedItem);

        // RemoveMatchingItemsFromHandStack( itemPackage, hand );

        // if ( itemPackage.packageType == ItemPackage.ItemPackageType.TwoHanded )
        // {
        //     RemoveMatchingItemsFromHandStack( itemPackage, hand.otherHand );
        // }
    }

    private void SpawnAndAttachObject( Hand hand, GrabTypes grabType )
    {
        if ( showTriggerHint )
        {
            hand.HideGrabHint();
        }

        ActivateFadedPreviewObject();
        spawnedItem = GameObject.Instantiate( buildingData.throwablePrefab );
        spawnedItem.SetActive( true );
        hand.AttachObject( spawnedItem, grabType, attachmentFlags );

        hand.ForceHoverUnlock();

        ThrowableBuilding throwableBuilding = spawnedItem.GetComponent<ThrowableBuilding>();
        throwableBuilding.rtsBuildingTypeData = buildingData;
        throwableBuilding.onDetachFromHand.AddListener(OnThrowableDetachedFromHand);
        itemIsSpawned = true;
        justPickedUpItem = true;

        pickupEvent?.Invoke();
    }

    private void OnThrowableDetachedFromHand()
    {
        ActivateNormalPreviewObject();
    }

    public static event EventHandler<BuildingPlacementEvent> OnBuildingPlacementEvent;
    public class BuildingPlacementEvent : Swordfish.Event
    {
        public BuildingData buildingData;  
        public Hand hand;      
    }
}

