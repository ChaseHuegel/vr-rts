using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using Swordfish.Audio;
using Swordfish.Navigation;
using MLAPI;

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
        bool cellsOccupied = false;
        Vector3 groundPosition = Vector3.zero;

        if ( hitPointValid )
        {
            ContactPoint contact = collision.contacts[0];

            float backTrackLength = 1f;
            Ray ray = new Ray(contact.point - (-contact.normal * backTrackLength), -contact.normal);
            //Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 5, true);
            groundPosition = contact.point;
            cellsOccupied = CellsOccupied(groundPosition, rtsBuildingTypeData.boundingDimensionX, rtsBuildingTypeData.boundingDimensionY);
        }      

        if (hitPointValid && !cellsOccupied)
        {
            GameObject spawned = GameObject.Instantiate(rtsBuildingTypeData.constructablePrefab);
            spawned.transform.position = groundPosition;
            spawned.transform.rotation = rtsBuildingTypeData.worldPrefab.transform.rotation;
            spawned.transform.Rotate(0f, 0f, Random.Range(0, 4) * 90);

            AudioSource.PlayClipAtPoint( placementAllowedAudio.GetClip(), groundPosition );

            // Remove resources only when valid placement.
            PlayerManager.instance.RemoveResourcesFromStockpile(rtsBuildingTypeData.goldCost,
                                                rtsBuildingTypeData.grainCost,
                                                rtsBuildingTypeData.woodCost,
                                                rtsBuildingTypeData.stoneCost);            
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

    private bool CellsOccupied(Vector3 position, int dimensionX, int dimensionY)
    {
        Cell cell = World.at(World.ToWorldCoord(position));

        int startX = cell.x - dimensionX / 2;
        int startY = cell.y - dimensionY / 2;
        int endX = startX + dimensionX;
        int endY = startY + dimensionY;

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++ )
            {
                Cell curCell = World.at(x, y);
                if (curCell.occupied)
                {
                    // Debug.Log(string.Format("x: {0} y: {1} name: {2}",
                    //         curCell.x, curCell.y, curCell.occupants[0].name));
                    return true;
                }
            }
        }

        return false;
    }

    private static bool LayerMatchTest(LayerMask layerMask, GameObject obj)
    {
        return ( ( 1 << obj.layer ) & layerMask ) != 0;
    }
}