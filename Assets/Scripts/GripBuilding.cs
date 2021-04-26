using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class GripBuilding : MonoBehaviour
{
    public Vector3 terrainScale;
    public GameObject terrainBuilding;
    

    private Interactable interactable;

    private Vector3 menuPosition;
    private Quaternion menuRotation;
    private Vector3 menuScale;

    private void Start()
    {

        interactable = this.GetComponent<Interactable>();

        menuPosition = this.transform.localPosition;
        menuRotation = this.transform.localRotation;
        menuScale = this.transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
        {
            if (interactable != null && interactable.attachedToHand != null) //don't explode in hand
                return;
            
            if (collision.gameObject.name == "Floor")
            {
                Vector3 groundPosition = collision.GetContact(0).point;
                groundPosition.y = 0;

                GameObject go = Instantiate(terrainBuilding, groundPosition, terrainBuilding.transform.rotation) as GameObject;                
                
                go.GetComponent<Rigidbody>().isKinematic = true;
                go.GetComponent<Interactable>().enabled = false;             
            }            
            else if (gameObject.GetComponent<Rigidbody>().useGravity)
            {
                this.gameObject.GetComponent<Rigidbody>().useGravity = false; 
                this.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                this.transform.localPosition = menuPosition;
                this.transform.localRotation = menuRotation;
                this.transform.localScale = menuScale;            
            }

            //Destroy(this.gameObject);
        }
}
