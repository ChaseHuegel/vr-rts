using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent( typeof( Interactable ) )]
public class BuildMenuSlot : MonoBehaviour
{
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

        if ( requireGrabActionToTake )
        {
            GrabTypes startingGrab = hand.GetGrabStarting();

            if (startingGrab != GrabTypes.None)
            {
                if (startingGrab == GrabTypes.Trigger || startingGrab == GrabTypes.Pinch)
                    SpawnAndAttachObject( hand, startingGrab);// GrabTypes.Scripted);
            }
        }
    }


    //-------------------------------------------------
    private void OnHandHoverEnd( Hand hand )
    {
        if ( !justPickedUpItem && requireGrabActionToTake && showTriggerHint )
        {
            hand.HideGrabHint();
        }

        justPickedUpItem = false;
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
        // if ( hand.otherHand != null )
        // {
        //     //If the other hand has this item package, take it back from the other hand
        //     ItemPackage otherHandItemPackage = GetAttachedItemPackage( hand.otherHand );
        //     if ( otherHandItemPackage == itemPackage )
        //     {
        //         TakeBackItem( hand.otherHand );
        //     }
        // }

        if ( showTriggerHint )
        {
            hand.HideGrabHint();
        }

        // if ( itemPackage.otherHandItemPrefab != null )
        // {
        //     if ( hand.otherHand.hoverLocked )
        //     {
        //         Debug.Log( "<b>[SteamVR Interaction]</b> Not attaching objects because other hand is hoverlocked and we can't deliver both items." );
        //         return;
        //     }
        // }

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

    // private void RemoveMatchingItemsFromHandStack( ItemPackage package, Hand hand )
    // {
    //     if (hand == null)
    //         return;

    //     for ( int i = 0; i < hand.AttachedObjects.Count; i++ )
    //     {
    //         ItemPackageReference packageReference = hand.AttachedObjects[i].attachedObject.GetComponent<ItemPackageReference>();
    //         if ( packageReference != null )
    //         {
    //             ItemPackage attachedObjectItemPackage = packageReference.itemPackage;
    //             if ( ( attachedObjectItemPackage != null ) && ( attachedObjectItemPackage == package ) )
    //             {
    //                 GameObject detachedItem = hand.AttachedObjects[i].attachedObject;
    //                 hand.DetachObject( detachedItem );
    //             }
    //         }
    //     }
    // }


    //-------------------------------------------------
    // private void RemoveMatchingItemTypesFromHand( ItemPackage.ItemPackageType packageType, Hand hand )
    // {
    //     for ( int i = 0; i < hand.AttachedObjects.Count; i++ )
    //     {
    //         ItemPackageReference packageReference = hand.AttachedObjects[i].attachedObject.GetComponent<ItemPackageReference>();
    //         if ( packageReference != null )
    //         {
    //             if ( packageReference.itemPackage.packageType == packageType )
    //             {
    //                 GameObject detachedItem = hand.AttachedObjects[i].attachedObject;
    //                 hand.DetachObject( detachedItem );
    //             }
    //         }
    //     }
    // }
}

