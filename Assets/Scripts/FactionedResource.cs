using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Changes the material for units and buildings based on the color chosen by 
/// the player for the objects it's attached to.
/// </summary>
public class FactionedResource : Resource
{
    public BuildingData buildingData;
    protected override void UpdateSkin()
    {
        if (SkinRendererTargets.Length <= 0) return;

        if (Faction?.skin?.buildingMaterial)
        {
            foreach (var renderer in SkinRendererTargets)
                renderer.sharedMaterial = Faction.skin.buildingMaterial;
        }
    }
}
