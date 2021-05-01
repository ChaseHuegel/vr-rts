using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HammerDestroyerOfWorlds : MonoBehaviour
{
    public GameObject hammerAttachmentPoint;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void PutAway()
    {
        gameObject.transform.SetParent(hammerAttachmentPoint.transform);
        gameObject.SetActive(false);
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
