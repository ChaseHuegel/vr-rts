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

    public GameObject objectDestroyedEffect;
    public GameObject unitDestroyedEffect;

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
            GameObject spawned = GameObject.Instantiate(objectDestroyedEffect);
            spawned.transform.position = other.transform.position;
            AudioSource.PlayClipAtPoint( destroyedObjectAudio, other.transform.position);

        }
        else if (other.gameObject.GetComponent<VillagerActor>())
        {
            GameObject spawned = GameObject.Instantiate(unitDestroyedEffect);
            spawned.transform.position = other.transform.position;
            audioSource.PlayOneShot(unitHitAudio);
        }
    }
}
