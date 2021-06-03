using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Changes the material for units and buildings based on the color chosen by 
/// the player for the objects it's attached to.
/// </summary>
public class FactionedResourceSkinner : MonoBehaviour
{
    public byte factionId;
    public MeshRenderer[] meshes;
    public SkinnedMeshRenderer[] skinnedMeshes;

    // Start is called before the first frame update
    void Start()
    {
        factionId = GetComponent<Resource>().factionId;
        SetSkin();        
    }

    void OnValidate()
    {
        GetComponent<Resource>().factionId = factionId;
        SetSkin();
    }

    private void SetSkin()
    {
        Faction faction = GameMaster.Factions.Find(x => x.Id == factionId); ;

        if (!faction) return;

        if (faction.skin.buildingMaterial)
        {
            foreach(MeshRenderer mesh in meshes)
                mesh.sharedMaterial = faction.skin.buildingMaterial;

            foreach(SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
                skinnedMesh.sharedMaterial = faction.skin.buildingMaterial;
        }
    }
}
