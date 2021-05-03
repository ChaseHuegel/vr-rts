using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HammerDestroyerOfWorlds : MonoBehaviour
{
    public GameObject hammerAttachmentPoint;

    public void PutAway()
    {
        gameObject.transform.SetParent(hammerAttachmentPoint.transform);        
        //gameObject.SetActive(false);
        gameObject.GetComponent<Rigidbody>().isKinematic = true;
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        if (other.gameObject.GetComponent<TerrainBuilding>())
        {
            Destroy(other.gameObject);
        }
    }
}
