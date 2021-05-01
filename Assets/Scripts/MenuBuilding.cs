using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class MenuBuilding : MonoBehaviour
{
    public Collider targetCollider;
    public GameObject spawnBuildingOnCollision;
    public bool destroyOnTargetCollision = true;
    
    private PalmMenuSlot palmMenuSlot;
    private bool canReAttach;
    private Interactable interactible;
    private bool hasBeenDetachedFromHand;

    void Start()
    {
        palmMenuSlot = this.GetComponentInParent<PalmMenuSlot>();
    }

    public void DetachedFromHand()
    {
        // Use gravity, kinematics off, etc..        
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
        gameObject.GetComponent<Rigidbody>().useGravity = true; 

        palmMenuSlot.RespawnMenuSlotObject();  
        hasBeenDetachedFromHand = true;
        // if (canReAttach)    
        // {
        //     gameObject.transform.parent = palmMenuSlot.transform;
        //     this.gameObject.GetComponent<Rigidbody>().useGravity = false; 
        //     gameObject.GetComponent<Rigidbody>().isKinematic = true;
        //     gameObject.GetComponent<Rigidbody>().isKinematic = false;
        //     gameObject.transform.localPosition = Vector3.zero;
        // }
        // else
        // {
        //     // Use gravity, kinematics off, etc..
        //     gameObject.GetComponent<Rigidbody>().useGravity = true; 
        //     gameObject.GetComponent<Rigidbody>().isKinematic = false;

        //     palmMenuSlot.RespawnMenuSlotObject();    
        // }        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasBeenDetachedFromHand)
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
        }

        Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 5, true);
        
        Destroy(this.gameObject);
    }
}
