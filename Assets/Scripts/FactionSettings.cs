using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionSettings : MonoBehaviour
{
    public MeshRenderer[] meshesToChange;
    public SkinnedMeshRenderer[] skinnedMeshesToChange;
    
    // Start is called before the first frame update
    void Start()
    {   
        SetSkin();
    }

    void OnValidate()
    {
        //SetSkin();
    }

    private void SetSkin()
    {
        Unit unit = GetComponent<Unit>();
        Material mat = null;
        if (unit)
        {
            mat = GameMaster.Instance.factions[unit.factionID].unitMaterial;
        }
        else
        {
            Structure structure = GetComponent<Structure>();
            if (!structure)
                Debug.Log("Structure component not found.", this);
            else
                mat = GameMaster.Instance.factions[structure.factionID].buildingMaterial;
        }

        if (mat)
        {
            foreach(MeshRenderer mesh in meshesToChange)
            {
                mesh.sharedMaterial = mat;
            }
            
            foreach(SkinnedMeshRenderer skinnedMesh in skinnedMeshesToChange)
            {
                skinnedMesh.sharedMaterial = mat;
            }
        }
    }
}
