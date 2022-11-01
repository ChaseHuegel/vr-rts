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


    public BuildingData rtsTypeData;
    public bool requireGrabActionToTake = true;
    public bool requireReleaseActionToReturn = false;
    public bool showTriggerHint = false;

    [EnumFlags]
    public Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags;

    private GameObject spawnedItem;
    private bool itemIsSpawned = false;
    private bool useFadedPreview;
    public UnityEvent pickupEvent;
    public UnityEvent dropEvent;
    private GameObject previewObject;
    public bool justPickedUpItem = false;
    public static Material disabledMat;
    public static Material enabledMat;
    SphereCollider grabCollider;    
    
    MeshRenderer[] meshRenderers;
    SkinnedMeshRenderer[] skinnedMeshRenderers;
    void Awake()
    {
        if (disabledMat == null)
                Debug.LogError("Disabled Material is missing", this);
        
        if (!(grabCollider = GetComponent<SphereCollider>()))
            Debug.Log("grabCollider missing.");    

        CreatePreviewObject();        
    }

    public void SlotEnabled(bool enabled = true)
    {
        if (!grabCollider)
            grabCollider = GetComponent<SphereCollider>();
        
        GetMeshRenderers();
        
        if (enabled)
        {
            grabCollider.enabled = true;

            if (previewObject)
                SetMaterial(enabledMat);
        }
        else
        {   
            grabCollider.enabled = false;  

            if(previewObject)
                SetMaterial(disabledMat);
        }
    }

    public void CreatePreviewObject()
    {
        ClearPreview();

        // Use normal preview
        if ( useFadedPreview == false )
        {             
            if (rtsTypeData.menuPreviewPrefab != null)
            {
                previewObject = Instantiate( rtsTypeData.menuPreviewPrefab, transform.position, Quaternion.identity ) as GameObject;
                previewObject.transform.parent = transform;
                previewObject.transform.localRotation = Quaternion.identity;
            }            
        }
        // Spawned item is being held, use faded preview
        else
        {
            if (rtsTypeData.fadedPreviewPrefab != null)
            {
                previewObject = Instantiate( rtsTypeData.fadedPreviewPrefab, transform.position, Quaternion.identity ) as GameObject;
                previewObject.transform.parent = transform;
                previewObject.transform.localRotation = Quaternion.identity;
            }
        }

        GetMeshRenderers();
    }

    private void GetMeshRenderers()
    {
        if (!previewObject)
            return;

        meshRenderers = previewObject.GetComponentsInChildren<MeshRenderer>();
        skinnedMeshRenderers = previewObject.GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    private void SetMaterial(Material material)
    {
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.sharedMaterial = material;
        }

        foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
        {
            skinnedMeshRenderer.sharedMaterial = material;
        }
    }

    private void ClearPreview()
    {
        GameObject[] allChildren = new GameObject [ transform.childCount ] ;

        int i = 0;

        foreach ( Transform child in transform )
        {
            allChildren [ i ] = child.gameObject;
            i++;
        }

        foreach ( GameObject child in allChildren )
        {
            if ( Time.time > 0  && !child.GetComponent<BuildMenuResouceCost>())
            {
                GameObject.Destroy( child.gameObject );
            }
            else if (!child.GetComponent<BuildMenuResouceCost>())
            {
                GameObject.DestroyImmediate( child.gameObject );
            }
        }
    }

    void Update()
    {
        if ( ( itemIsSpawned == true ) && ( spawnedItem == null ) )
        {
            itemIsSpawned = false;
            useFadedPreview = false;
            dropEvent.Invoke();
            CreatePreviewObject();
        }                  
    }

    private void ScaleUp()
    {
        transform.localScale = new Vector3(1.25f, 1.25f, 1.25f);
    }
    
    private void ResetScale()
    {
        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    private void OnHandHoverBegin( Hand hand )
    {
        ThrowableBuilding currentAttachedThrowableBuilding = GetAttachedThrowableBuilding( hand );
    
        if ( currentAttachedThrowableBuilding )
        {
            // If we want to take back the item and we aren't waiting for a trigger press
            if ( !requireReleaseActionToReturn && !justPickedUpItem)
            {
                if(rtsTypeData.buildingType == currentAttachedThrowableBuilding.rtsBuildingTypeData.buildingType)
                    TakeBackItem( hand );
            }
        }

        ScaleUp();

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

                    // So that we don't pick up an throwable building the same frame that we return it
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
                    SpawnAndAttachObject( hand, startingGrab);// GrabTypes.Scripted);
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
                
                BuildingPlacementEvent e = new BuildingPlacementEvent{ buildingData = rtsTypeData, hand = hand };
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
        {
            hand.HideGrabHint();
        }

        justPickedUpItem = false;
        ResetScale();
    }

    private ThrowableBuilding GetAttachedThrowableBuilding( Hand hand )
    {
        GameObject currentAttachedObject = hand.currentAttachedObject;

        // Verify the hand is holding something
        if ( currentAttachedObject == null )
        {
            return null;
        }

        ThrowableBuilding throwableBuilding = hand.currentAttachedObject.GetComponent<ThrowableBuilding>();

        // Verify the item in the hand is a throwable building
        if ( throwableBuilding == null )
        {
            return null;
        }

        return throwableBuilding;
    }

    private void TakeBackItem( Hand hand )
    {        
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

        spawnedItem = GameObject.Instantiate( rtsTypeData.throwablePrefab );
        spawnedItem.SetActive( true );
        hand.AttachObject( spawnedItem, grabType, attachmentFlags );

        hand.ForceHoverUnlock();

        spawnedItem.GetComponent<ThrowableBuilding>().rtsBuildingTypeData = rtsTypeData;

        itemIsSpawned = true;
        justPickedUpItem = true;

        useFadedPreview = true;
        pickupEvent.Invoke();
        CreatePreviewObject();
    }

    public static event EventHandler<BuildingPlacementEvent> OnBuildingPlacementEvent;
    public class BuildingPlacementEvent : Swordfish.Event
    {
        public BuildingData buildingData;  
        public Hand hand;      
    }
}

