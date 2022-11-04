using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Swordfish.Navigation;

[RequireComponent(typeof ( Interactable ))]
public class MenuBuilding : Throwable
{    public GameObject spawnBuildingOnCollision;
    public bool destroyOnTargetCollision = true;

    private BuildMenuSlot buildMenuSlot;
    private bool canReAttach;
    private Interactable interactible;

    [Header ("Placement Preview Visuals")]
    public GameObject laser;
    public float previewObjectLocalScale = 0.09f;
    public LayerMask allowedLayersMask;
    public LayerMask disallowedLayersMask;
    private RaycastHit hitInfo;
    private bool pinchGrip;
    public SteamVR_Action_Boolean rotatePreviewClockwise;
    public SteamVR_Action_Boolean rotatePreviewCounterClockwise;
    //public SteamVR_Action_Boolean buildingPlacementPointer;

    void Start()
    {
        buildMenuSlot = gameObject.GetComponentInParent<BuildMenuSlot>();
        interactible = gameObject.GetComponent<Interactable>();

        rotatePreviewClockwise.AddOnStateDownListener(RotatePreviewClockwiseDown, SteamVR_Input_Sources.RightHand);
        rotatePreviewCounterClockwise.AddOnStateDownListener(RotatePreviewCounterClockwiseDown, SteamVR_Input_Sources.RightHand);

        // buildingPlacementPointer.AddOnStateDownListener(BuildingPlacementPointerDown, SteamVR_Input_Sources.RightHand);
        // buildingPlacementPointer.AddOnStateUpListener(BuildingPlacementPointerUp, SteamVR_Input_Sources.RightHand);
        // buildingPlacementPointer.AddOnStateDownListener(BuildingPlacementPointerDown, SteamVR_Input_Sources.LeftHand);
        // buildingPlacementPointer.AddOnStateUpListener(BuildingPlacementPointerUp, SteamVR_Input_Sources.LeftHand);
    }

    public void RotatePreviewCounterClockwiseDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (interactible.attachedToHand)
            spawnBuildingOnCollision.transform.Rotate(0f, 0f, -45);

        //Debug.Log("rotated " + spawnBuildingOnCollision.gameObject.name);
    }

    public void RotatePreviewClockwiseDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (interactible.attachedToHand)
            spawnBuildingOnCollision.transform.Rotate(0f, 0f, 45);

        //Debug.Log("rotated " + spawnBuildingOnCollision.gameObject.name);
    }

    // public void BuildingPlacementPointerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    // {
    //     // if (interactible.isHovering)
    //     // {
    //         spawnBuildingOnCollision = GameObject.Instantiate(spawnBuildingOnCollision);
    //         spawnBuildingOnCollision.transform.localScale = new Vector3(previewObjectLocalScale, previewObjectLocalScale, previewObjectLocalScale);
    //         spawnBuildingOnCollision.gameObject.layer = LayerMask.NameToLayer("UI");
    //         spawnBuildingOnCollision.GetComponent<BoxCollider>().enabled = false;
    //         spawnBuildingOnCollision.GetComponent<TerrainBuilding>().enabled = false;

    //         // TODO: Enable for snapping while placing?
    //         spawnBuildingOnCollision.GetComponent<Obstacle>().enabled = false;

    //         ObjectPlacementPointer.instance.SetDestinationReticle(spawnBuildingOnCollision);
    //         Debug.Log("pointer button down");
    //     // }
    // }

    // public void BuildingPlacementPointerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    // {

    //     spawnBuildingOnCollision.transform.SetParent(null);
    //     spawnBuildingOnCollision.GetComponent<BoxCollider>().enabled = true;
    //     spawnBuildingOnCollision.GetComponent<Obstacle>().enabled = true;
    //     spawnBuildingOnCollision.GetComponent<TerrainBuilding>().enabled = true;
    //     spawnBuildingOnCollision.gameObject.layer = LayerMask.NameToLayer("Building");

    //     // Use gravity, kinematics off, etc..
    //     gameObject.GetComponent<Rigidbody>().isKinematic = false;
    //     gameObject.GetComponent<Rigidbody>().useGravity = true;
    //     gameObject.GetComponentInChildren<Collider>().isTrigger = false;

    //     buildingPlacementPointer.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
    //     rotatePreviewClockwise.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
    //     rotatePreviewCounterClockwise.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
    //     rotatePreviewCounterClockwise.RemoveAllListeners(SteamVR_Input_Sources.LeftHand);
    //     //Destroy(base.gameObject);
    // }

    protected override void HandAttachedUpdate(Hand hand)
    {
        base.HandAttachedUpdate(hand);

        // Debug.DrawRay(hand.hoverSphereTransform.position, hand.hoverSphereTransform.forward, Color.red);
        //Debug.DrawRay(hand.objectAttachmentPoint.position, hand.objectAttachmentPoint.forward, Color.blue);

        if (interactable.skeletonPoser != null)
        {
            //if (hand.currentAttachedObject)
                pinchGrip = hand.currentAttachedObjectInfo.Value.grabbedWithType == GrabTypes.Pinch;

            // if (pinchGrip)
            // {
            //     interactable.skeletonPoser.SetBlendingBehaviourEnabled("PinchPose", true);

            //     Transform origin = Player.instance.rightHand.GetComponent<HandTrackingPoint>().trackingPoint.transform;

            //     // Use vertical laser placement method
            //     if ( origin.transform.up.y > 0.9f)
            //     {
            //         InteractionPointer.instance.StopPlacement(hand);
            //         if ( Physics.Raycast( transform.position, Vector3.down, out hitInfo, 100, allowedLayersMask ) )
            //         {
            //             // Are we hitting something on acceptable layer?
            //             bool hitPointValid = !LayerMatchTest( disallowedLayersMask, hitInfo.collider.gameObject );

            //             if (hitPointValid)
            //             {
            //                 PointLaser();
            //             }
            //             else
            //             {
            //                 DisplayLaserAndPreviewObject(false);

            //             }
            //         }
            //     }
            //     // Use arcing laser placement method
            //     else
            //     {
            //         DisplayLaserAndPreviewObject(false);
            //         InteractionPointer.instance.StartPlacement(hand);
            //         spawnBuildingOnCollision.transform.localScale = new Vector3(previewObjectLocalScale, previewObjectLocalScale, previewObjectLocalScale);
            //         spawnBuildingOnCollision.transform.position = InteractionPointer.instance.destinationReticleTransform.position;

            //     }
            // }
            // else // Grab grip
            // {
            //     DisplayLaserAndPreviewObject(false);
            // }
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

        spawnBuildingOnCollision.transform.localScale = new Vector3(previewObjectLocalScale, previewObjectLocalScale, previewObjectLocalScale);
        spawnBuildingOnCollision.transform.position = hitInfo.point;
    }

    private static bool LayerMatchTest(LayerMask layerMask, GameObject obj)
    {
        return ( ( 1 << obj.layer ) & layerMask ) != 0;
    }

    private void DisplayLaserAndPreviewObject(bool show)
    {
        laser.SetActive(show);
    }

    protected override void OnDetachedFromHand(Hand hand)
    {
        base.OnDetachedFromHand(hand);

        //buildMenuSlot.RespawnMenuSlotObject();

        if (pinchGrip)
        {
            // Need to check if position is valid
            spawnBuildingOnCollision.transform.SetParent(null);
            spawnBuildingOnCollision.GetComponent<BoxCollider>().enabled = true;
            spawnBuildingOnCollision.GetComponent<Obstacle>().enabled = true;
            spawnBuildingOnCollision.GetComponent<TerrainBuilding>().enabled = true;
            SetLayer(spawnBuildingOnCollision, "Building");
            InteractionPointer.instance.StopPlacement(hand);

            BuildingData buildingData = GameMaster.GetBuilding(buildMenuSlot.rtsTypeData.buildingType);

            PlayerManager.Instance.DeductResourcesFromStockpile(buildingData.goldCost, buildingData.foodCost,
                                               buildingData.woodCost, buildingData.stoneCost);


        }


        // Use gravity, kinematics off, etc..
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
        gameObject.GetComponent<Rigidbody>().useGravity = true;
        gameObject.GetComponentInChildren<Collider>().isTrigger = false;

        Destroy(laser.gameObject);

        if (pinchGrip)
        {
            // buildingPlacementPointer.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
            rotatePreviewClockwise.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
            rotatePreviewCounterClockwise.RemoveAllListeners(SteamVR_Input_Sources.RightHand);
            rotatePreviewCounterClockwise.RemoveAllListeners(SteamVR_Input_Sources.LeftHand);
            Destroy(base.gameObject);
        }

    }

    void SetLayer(GameObject go, string layer)
    {
        go.gameObject.layer = LayerMask.NameToLayer(layer);
        foreach(Transform child in go.transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer(layer);
        }
    }

    protected override void OnAttachedToHand(Hand hand)
    {
        base.OnAttachedToHand(hand);

        //GameObject.Instantiate(previewObject, ObjectPlacementPointer.instance.destinationReticleTransform );

        // TODO: this is too much instantiation, must fix at later date.
        spawnBuildingOnCollision = GameObject.Instantiate(spawnBuildingOnCollision);
        spawnBuildingOnCollision.transform.localScale = new Vector3(previewObjectLocalScale, previewObjectLocalScale, previewObjectLocalScale);
        //spawnBuildingOnCollision.gameObject.layer = LayerMask.NameToLayer("UI");
        SetLayer(spawnBuildingOnCollision, "UI");
        spawnBuildingOnCollision.GetComponent<BoxCollider>().enabled = false;
        spawnBuildingOnCollision.GetComponent<TerrainBuilding>().enabled = false;

        // TODO: Enable for snapping while placing?
        spawnBuildingOnCollision.GetComponent<Obstacle>().enabled = false;

        // Determine angle of hand and then decide which laser to show?
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
        //Destroy(previewObject.gameObject);
        Destroy(this.gameObject);
    }
}
