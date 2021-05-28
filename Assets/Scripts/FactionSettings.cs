using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionSettings : MonoBehaviour
{
    public bool overrideFaction = false;
    public int factionId = 0;

    public MeshRenderer[] meshesToChange;
    public SkinnedMeshRenderer[] skinnedMeshesToChange;
    
    private bool isUnit;

    // Start is called before the first frame update
    void Start()
    {   
        if (!overrideFaction)
        {            
            Unit unit = GetComponent<Unit>();
            if (unit)
            {
                isUnit = true;
                factionId = unit.factionId;
            }
            else
            {
                Structure structure = GetComponent<Structure>();
                if (structure)
                    factionId = structure.factionId;  
                else
                {
                    factionId = GetComponent<FactionSettings>().factionId;
                }
            }
        } 
        
        SetSkin();
    }

    void OnValidate()
    {
        // Start();
        // SetSkin();
    }

    private void SetSkin()
    {
        if (!GameMaster.Instance)
            return;
            
        Material mat = null;
        if (isUnit)
            mat = GameMaster.Instance.factions[factionId].unitMaterial;
        else if (!isUnit)
            mat = GameMaster.Instance.factions[factionId].buildingMaterial;

        if (mat)
        {
            foreach(MeshRenderer mesh in meshesToChange)
                mesh.sharedMaterial = mat;
            
            foreach(SkinnedMeshRenderer skinnedMesh in skinnedMeshesToChange)
                skinnedMesh.sharedMaterial = mat;
        }
    }
}
