using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class HammerDestroyerOfWorlds : MonoBehaviour
{
    public GameObject hammerAttachmentPoint;
    public AudioClip destroyedObjectAudio;
    public AudioClip unitHitAudio;
    AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
    }
    
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
        if (other.gameObject.GetComponent<TerrainBuilding>())
        {
            Destroy(other.gameObject);
            audioSource.PlayOneShot(destroyedObjectAudio);
        }
        else if (other.gameObject.GetComponent<VillagerActor>())
        {
            audioSource.PlayOneShot(unitHitAudio);
        }
    }
}
