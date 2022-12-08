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

    protected override void Start()
    {
        base.Start();
        OnLoadBuildingData(buildingData);
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeType.YIELD, 100f, 100f);
    }

    protected virtual void OnLoadBuildingData(BuildingData data)
    {
        Attributes.AddOrUpdate(AttributeType.YIELD, this.yield, this.yield);

        if (PlayerManager.AllBuildingAttributeBonuses.TryGetValue(data, out var bonuses))
            foreach (StatUpgradeContainer bonus in bonuses)
                Attributes.Get(bonus.targetAttribute).AddModifier(bonus.targetAttribute, bonus.modifier, bonus.amount);
    }
    
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
