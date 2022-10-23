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
    public Vector3 wieldLocalScale;
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

    void OnTriggerEnter(Collider collider)
    {
        if (!isWielding)
            return;

        Structure structure = collider.gameObject.GetComponentInParent<Structure>();
        if (structure)
        {
            Vector3 pos = new Vector3(collider.transform.position.x, 0, collider.transform.position.z);
            Destroy(structure.gameObject);

            // TODO: Shoot a ray down to find a ground position.
            GameObject spawned = GameObject.Instantiate(objectDestroyedEffect, pos, Quaternion.identity);
            AudioSource.PlayClipAtPoint(destroyedObjectAudio, pos);
            return;
        }

        Constructible constructible = collider.GetComponentInParent<Constructible>();
        if (constructible)
        {
            Vector3 pos = new Vector3(collider.transform.position.x, 0, collider.transform.position.z);
            Destroy(constructible.gameObject);

            // TODO: Shoot a ray down to find a ground position.
            GameObject spawned = GameObject.Instantiate(objectDestroyedEffect, pos, Quaternion.identity);
            AudioSource.PlayClipAtPoint(destroyedObjectAudio, pos);
            return;
        }

        if (collider.gameObject.GetComponent<UnitV2>())
        {
            GameObject spawned = GameObject.Instantiate(unitDestroyedEffect);
            spawned.transform.position = collider.transform.position;
            audioSource.PlayOneShot(unitHitAudio);
        }
    }
}
