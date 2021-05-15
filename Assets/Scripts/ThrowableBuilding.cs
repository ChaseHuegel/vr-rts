using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using Swordfish.Audio;

public class ThrowableBuilding : Throwable
{
    //public GameObject worldPrefabToSpawn;
    public GameObject placementDeniedEffect;
    public SoundElement placementDeniedAudio;
    public SoundElement placementAllowedAudio;
    //public LayerMask allowedLayersMask;
    public LayerMask disallowedLayersMask;
    public BuildingData rtsBuildingTypeData;
    private void OnCollisionEnter(Collision collision)
    {
        bool hitPointValid = !LayerMatchTest( disallowedLayersMask, collision.gameObject );

        if ( hitPointValid )
        {
            ContactPoint contact = collision.contacts[0];

            float backTrackLength = 1f;
            Ray ray = new Ray(contact.point - (-contact.normal * backTrackLength), -contact.normal);

            Vector3 groundPosition = contact.point;
            //groundPosition.y = 0;

            GameObject spawned = GameObject.Instantiate(rtsBuildingTypeData.worldPrefab);
            spawned.transform.position = groundPosition;
            spawned.transform.rotation = rtsBuildingTypeData.worldPrefab.transform.rotation;
            spawned.transform.Rotate(0f, 0f, Random.Range(0, 4) * 90);

            AudioSource.PlayClipAtPoint( placementAllowedAudio.GetClip(), contact.point );

            // Remove resources only when valid placement.
            PlayerManager.instance.RemoveResourcesFromStockpile(rtsBuildingTypeData.goldCost,
                                                rtsBuildingTypeData.grainCost,
                                                rtsBuildingTypeData.woodCost,
                                                rtsBuildingTypeData.stoneCost);

            Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 5, true);
        }
        else
        {
            ContactPoint contact = collision.contacts[0];
            GameObject spawned = GameObject.Instantiate(placementDeniedEffect);
            spawned.transform.position = contact.point;
            AudioSource.PlayClipAtPoint( placementDeniedAudio.GetClip(),contact.point );
        }

        Destroy(this.gameObject);
    }
    private static bool LayerMatchTest(LayerMask layerMask, GameObject obj)
    {
        return ( ( 1 << obj.layer ) & layerMask ) != 0;
    }
}