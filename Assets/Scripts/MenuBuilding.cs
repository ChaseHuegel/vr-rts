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
    void Start()
    {
        palmMenuSlot = this.GetComponentInParent<PalmMenuSlot>();
    }

    public void DetachedFromHand()
    {
        Debug.Log("detaching");
        // Use gravity, kinematics off, etc..        
        gameObject.GetComponent<Rigidbody>().isKinematic = false;
        gameObject.GetComponent<Rigidbody>().useGravity = true; 

        palmMenuSlot.RespawnMenuSlotObject();  

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

    void OnTriggerEnter(Collider collision)
    {
        // if (collision.gameObject.name == "ButtonTrigger")
        //     canReAttach = true;
        
        //Debug.Log(collision.transform.name + " Trigger");
    }

    void OnTriggerExit(Collider collision)
    {
        // if (collision.gameObject.name == "ButtonTrigger")
        //     canReAttach = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        float backTrackLength = 1f;
        Ray ray = new Ray(contact.point - (-contact.normal * backTrackLength), -contact.normal);
        Vector3 groundPosition = contact.point;
        groundPosition.y = 0;    
        GameObject spawned = GameObject.Instantiate(spawnBuildingOnCollision);                
        spawned.transform.position = groundPosition;
        spawned.transform.rotation = spawnBuildingOnCollision.transform.rotation;
        Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 5, true);
        
        Destroy(this.gameObject);

        // if (collision.collider == targetCollider)
        // {
        //     ContactPoint contact = collision.contacts[0];
        //     RaycastHit hit;

        //     float backTrackLength = 1f;
        //     Ray ray = new Ray(contact.point - (-contact.normal * backTrackLength), -contact.normal);
        //     if (collision.collider.Raycast(ray, out hit, 2))
        //     {            
        //         Vector3 groundPosition = contact.point;
        //         groundPosition.y = 0;    
        //         GameObject spawned = GameObject.Instantiate(spawnBuildingOnCollision);                
        //         spawned.transform.position = groundPosition;
        //         spawned.transform.rotation = spawnBuildingOnCollision.transform.rotation;
        //     }

        //     Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 5, true);

        //     if (destroyOnTargetCollision)
        //          Destroy(this.gameObject);
        // }
    }
}
