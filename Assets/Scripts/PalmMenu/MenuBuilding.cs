using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof ( Interactable ))]
public class MenuBuilding : Throwable
{    public GameObject spawnBuildingOnCollision;
    public bool destroyOnTargetCollision = true;
    
    private PalmMenuSlot palmMenuSlot;
    private bool canReAttach;
    private Interactable interactible;
    
    [Header ("Placement Preview Visuals")]
    public GameObject laser;
    public GameObject previewObject;
    public float previewObjectLocalScale = 0.09f;
    public LayerMask allowedLayersMask;
    public LayerMask disallowedLayersMask;      
    private RaycastHit hitInfo;
    private bool pinchGrip;
    public SteamVR_Action_Boolean rotatePreviewClockwise;
    public SteamVR_Action_Boolean rotatePreviewCounterClockwise;
    public SteamVR_Action_Boolean buildingPlacementPointer;
    private int rotationDirection = 0;

    void Start()
    {
        palmMenuSlot = gameObject.GetComponentInParent<PalmMenuSlot>();       
        interactible = gameObject.GetComponent<Interactable>(); 
      
        rotatePreviewClockwise.AddOnStateDownListener(RotatePreviewClockwiseDown, SteamVR_Input_Sources.RightHand);
        rotatePreviewCounterClockwise.AddOnStateDownListener(RotatePreviewCounterClockwiseDown, SteamVR_Input_Sources.RightHand);
        
        buildingPlacementPointer.AddOnStateDownListener(BuildingPlacementPointerDown, SteamVR_Input_Sources.RightHand);
        buildingPlacementPointer.AddOnStateUpListener(BuildingPlacementPointerUp, SteamVR_Input_Sources.RightHand);
        buildingPlacementPointer.AddOnStateDownListener(BuildingPlacementPointerDown, SteamVR_Input_Sources.LeftHand);
        buildingPlacementPointer.AddOnStateUpListener(BuildingPlacementPointerUp, SteamVR_Input_Sources.LeftHand);
    }
   
    public void RotatePreviewCounterClockwiseDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        previewObject.transform.Rotate(0f, 0f, -90);
    } 

    public void RotatePreviewClockwiseDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        previewObject.transform.Rotate(0f, 0f, 90);
    } 

    public void BuildingPlacementPointerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        //ObjectPlacementPointer.instance.SetDestinationReticle(previewObject);
        //Debug.Log("button down");
    }

    public void BuildingPlacementPointerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        
    }

    protected override void HandAttachedUpdate(Hand hand)
    {
        base.HandAttachedUpdate(hand);

        if (interactable.skeletonPoser != null)
        {
            pinchGrip = hand.currentAttachedObjectInfo.Value.grabbedWithType == GrabTypes.Pinch;

            if (pinchGrip)
            {
                interactable.skeletonPoser.SetBlendingBehaviourEnabled("PinchPose", true);

                if ( Physics.Raycast( transform.position, Vector3.down, out hitInfo, 100, allowedLayersMask ) )
                {
                    // Are we hitting something on acceptable layer?
                    bool hitPointValid = !LayerMatchTest( disallowedLayersMask, hitInfo.collider.gameObject );

                    if (hitPointValid)
                    {
                        PointLaser();
                    }
                    else
                    {
                        DisplayLaserAndPreviewObject(false);
                        
                    }
                }
            }
            else // Grab grip
            {
                DisplayLaserAndPreviewObject(false);
            }
        }
    }

    // protected override void HandHoverUpdate(Hand hand)
    // {
    //     base.HandHoverUpdate(hand);

        // GrabTypes startingGrabType = hand.GetGrabStarting();

        // if (startingGrabType != GrabTypes.None)
        // {
        //     if (startingGrabType == GrabTypes.Pinch)
        //     {
        //         hand.AttachObject(gameObject, startingGrabType, attachmentFlags, pinchOffset);
        //     }
        //     else if (startingGrabType == GrabTypes.Grip)
        //     {
        //         hand.AttachObject(gameObject, startingGrabType, attachmentFlags, gripOffset);
        //     }
        //     else
        //     {
        //         hand.AttachObject(gameObject, startingGrabType, attachmentFlags, attachmentOffset);
        //     }

        //     hand.HideGrabHint();
        // }
    // }

    private void PointLaser()
    {
        DisplayLaserAndPreviewObject(true);

        laser.transform.position = Vector3.Lerp(transform.position, hitInfo.point, .5f);
        
        // Point the laser at position where raycast hits.
        laser.transform.LookAt( hitInfo.point);

        // Scale the laser so it fits perfectly between the two positions
        laser.transform.localScale = new Vector3(laser.transform.localScale.x, 
            laser.transform.localScale.y, hitInfo.distance);
        
        previewObject.transform.localScale = new Vector3(previewObjectLocalScale, previewObjectLocalScale, previewObjectLocalScale);
        previewObject.transform.position = hitInfo.point;
    }

    private static bool LayerMatchTest(LayerMask layerMask, GameObject obj)
    {
        return ( ( 1 << obj.layer ) & layerMask ) != 0;
    }

    private void DisplayLaserAndPreviewObject(bool show)
    {
        laser.SetActive(show);
        previewObject.SetActive(show);        
    }

    protected override void OnDetachedFromHand(Hand hand)
    {
        base.OnDetachedFromHand(hand);

        palmMenuSlot.RespawnMenuSlotObject(); 
        
        if (pinchGrip)
        {
            GameObject spawned = GameObject.Instantiate(spawnBuildingOnCollision);
            spawned.transform.position = previewObject.transform.position;
            spawned.transform.rotation = previewObject.transform.rotation;
        }

        // Use gravity, kinematics off, etc..        
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
        gameObject.GetComponent<Rigidbody>().useGravity = true;          
        gameObject.GetComponentInChildren<Collider>().isTrigger = false;
        
        Destroy(laser.gameObject);

        if (pinchGrip)
        {
            rotatePreviewClockwise.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
            rotatePreviewCounterClockwise.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
            Destroy(previewObject.gameObject);
            Destroy(base.gameObject);
        }
        
    }
    
    protected override void OnAttachedToHand(Hand hand)
    {   
        base.OnAttachedToHand(hand);

        //GameObject.Instantiate(previewObject, ObjectPlacementPointer.instance.destinationReticleTransform );  
        
        // TODO: this is too much instantiation, must fix at later date.
        previewObject = GameObject.Instantiate(previewObject);   
        previewObject.transform.localScale = new Vector3(previewObjectLocalScale, previewObjectLocalScale, previewObjectLocalScale);
        previewObject.gameObject.layer = LayerMask.NameToLayer("UI");
        laser = GameObject.Instantiate(laser);        
    }    

    private void OnCollisionEnter(Collision collision)
    {
        if (interactible.attachedToHand)
            return;

        ContactPoint contact = collision.contacts[0];
        float backTrackLength = 1f;
        Ray ray = new Ray(contact.point - (-contact.normal * backTrackLength), -contact.normal);
        Vector3 groundPosition = contact.point;
        groundPosition.y = 0;    

        // TODO: There are still cases where buildings can spawn on top of each other
        if (collision.transform.name == "Floor")
        {
            GameObject spawned = GameObject.Instantiate(spawnBuildingOnCollision);
            spawned.transform.position = groundPosition;
            spawned.transform.rotation = spawnBuildingOnCollision.transform.rotation;
            spawned.transform.Rotate(0f, 0f, Random.Range(0, 4) * 90);
        }

        Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 5, true);
        
        rotatePreviewClockwise.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
        rotatePreviewCounterClockwise.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
        Destroy(previewObject.gameObject);  
        Destroy(this.gameObject);
    }
}