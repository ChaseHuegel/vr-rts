using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionSettings : MonoBehaviour
{
    public MeshRenderer[] meshesToChange;

    // Start is called before the first frame update
    void Start()
    {   
        SetSkin();
    }

    void OnValidate()
    {
        SetSkin();
    }

    private void SetSkin()
    {
        int factionID = GetComponent<Structure>().factionID;

        Material mat = GameMaster.Instance.factions[factionID].buildingMaterial;

        foreach(MeshRenderer mesh in meshesToChange)
        {
            mesh.sharedMaterial = mat;
        }
    }
}
