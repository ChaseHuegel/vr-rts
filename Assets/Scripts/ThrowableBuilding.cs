using System.Collections;
using System.Collections.Generic;
using MLAPI;
using Swordfish.Audio;
using Swordfish.Navigation;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class ThrowableBuilding : Throwable
{
    //public LayerMask allowedLayersMask;
    public LayerMask disallowedLayersMask;
    public BuildingData rtsBuildingTypeData;
    private void OnCollisionEnter(Collision collision)
    {
        bool hitPointValid = !LayerMatchTest(disallowedLayersMask, collision.gameObject);
        Vector3 groundPosition = Vector3.zero;

        if (hitPointValid)
        {
            ContactPoint contact = collision.GetContact(0);

            // float backTrackLength = 1f;
            // Ray ray = new Ray(contact.point - (-contact.normal * backTrackLength), -contact.normal);
            // Debug.DrawRay(ray.origin, ray.direction, Color.cyan, 5, true);
            groundPosition = contact.point;
        }

        // TODO: Move function to a library?        
        hitPointValid &= !CellsOccupied(groundPosition, rtsBuildingTypeData.boundingDimensionX, rtsBuildingTypeData.boundingDimensionY);
        
        if (hitPointValid)
        {
            Quaternion rotation = Quaternion.AngleAxis(Random.Range(0, 4) * 90, Vector3.up);
            GameObject spawned = Instantiate(rtsBuildingTypeData.constructionPrefab, groundPosition, rotation);

            // Thrown buildings are the local player's faction
           spawned.GetComponent<Constructible>().Faction = PlayerManager.Instance.faction;

            // TODO: Switch this to a 'construction started' sound rather than placement allowed
            InteractionPointer.instance.PlayBuildingPlacementAllowedAudio();

            // Remove resources only when valid placement.
            PlayerManager.Instance.DeductTechResourceCost(rtsBuildingTypeData);
        }
        else
        {
            ContactPoint contact = collision.contacts[0];
            GameObject spawned = GameObject.Instantiate(GameMaster.Instance.buildingPlacementDeniedFX);
            spawned.transform.position = contact.point;

            // TODO: Not sure if this should be handled by the playermanager or interaction pointer
            InteractionPointer.instance.PlayBuildingPlacementDeniedAudio();
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
            for (int y = startY; y < endY; y++)
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
        return ((1 << obj.layer) & layerMask) != 0;
    }
}