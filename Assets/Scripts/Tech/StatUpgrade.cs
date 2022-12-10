using System;
using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Library.Collections;
using Swordfish.Library.Types;
using UnityEngine;

[Serializable]
public class StatUpgradeContainer
{
    public List<BuildingData> targetBuildings;
    public List<UnitData> targetUnits;    
    public AttributeType targetAttribute;
    public Modifier modifier = Modifier.ADDITION;
    public float amount;

    public StatUpgradeContainer(AttributeType attributeType, Modifier modifierType, float fAmount)
    {
        targetAttribute = attributeType;
        modifier = modifierType;
        amount = fAmount;
    }

    public StatUpgradeContainer(StatUpgradeContainer bonus)
    {
        targetAttribute = bonus.targetAttribute;
        modifier = bonus.modifier;
        amount = bonus.amount;
    }
}

[CreateAssetMenu(fileName = "Stat Upgrade", menuName = "RTS/Tech/Stat Upgrade")]
public class StatUpgrade : TechBase
{
    public List<StatUpgradeContainer> statUpgrades;

    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        foreach (StatUpgradeContainer attributeBonus in statUpgrades)
        {
            foreach (UnitData unitData in attributeBonus.targetUnits)
                PlayerManager.Instance.AddUnitStatUpgrade(unitData, attributeBonus);

            foreach (BuildingData buildingData in attributeBonus.targetBuildings)
                PlayerManager.Instance.AddBuildingStatUpgrade(buildingData, attributeBonus);
        }
    }
}
