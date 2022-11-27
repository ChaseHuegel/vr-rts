using System;
using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Library.Collections;
using Swordfish.Library.Types;
using UnityEngine;

[Serializable]
public class AttributeBonus
{
    public AttributeType targetAttribute;
    public Modifier modifier = Modifier.ADDITION;
    public float amount;

    public AttributeBonus(AttributeType attributeType, Modifier modifierType, float fAmount)
    {
        targetAttribute = attributeType;
        modifier = modifierType;
        amount = fAmount;
    }
}

[CreateAssetMenu(fileName = "Stat Upgrade", menuName = "RTS/Tech/Stat Upgrade")]
public class StatUpgrade : TechBase
{
    public List<UnitData> targetUnits;
    public List<AttributeBonus> attributeBonuses;

    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        foreach (UnitData unitData in targetUnits)
            foreach (AttributeBonus attributeBonus in attributeBonuses)
                PlayerManager.Instance.AddUnitStatUpgrade(unitData, attributeBonus);

        Debug.Log(this.title + "research completed.");
    }
}
