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
    private Vector3 sheathLocalScale;
    public  Vector3 wieldLocalScale;
    protected bool isWielding;
    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        sheathLocalScale = transform.localScale;
    }
    
    public void Wield()
    {
        gameObject.transform.localScale = wieldLocalScale;  
        isWielding = true;
    }

    public void Sheath()
    {
        //gameObject.transform.SetParent(hammerAttachmentPoint.transform);        
        //gameObject.SetActive(false);
        gameObject.GetComponent<Rigidbody>().isKinematic = true;
        gameObject.transform.localPosition = Vector3.zero;
        gameObject.transform.localRotation = Quaternion.identity; 
        gameObject.transform.localScale = sheathLocalScale;
        isWielding = false;
    }

    void OnTriggerEnter(Collider other)
    {    
        if (!isWielding)
            return;
            
        Structure structure = other.gameObject.GetComponentInParent<Structure>();
        if (structure)
        {
            Destroy(structure.gameObject);

            // TODO: Shoot a ray down to find a ground position.
            Vector3 pos = new Vector3(structure.transform.position.x, 0, structure.transform.position.z);
            GameObject spawned = GameObject.Instantiate(objectDestroyedEffect, pos, Quaternion.identity);
            AudioSource.PlayClipAtPoint( destroyedObjectAudio, structure.transform.position);

        }
        else if (other.gameObject.GetComponent<Villager>())
        {
            GameObject spawned = GameObject.Instantiate(unitDestroyedEffect);
            spawned.transform.position = other.transform.position;
            audioSource.PlayOneShot(unitHitAudio);
        }
    }
}
