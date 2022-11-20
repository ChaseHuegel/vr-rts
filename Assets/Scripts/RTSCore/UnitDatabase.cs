using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit Database", menuName = "RTS/Units/Database")]
public class UnitDatabase : ScriptableObject
{
    [SerializeField] private List<UnitData> database = new List<UnitData>();

    // public UnitData Get(UnitData type)
    // {
    //     return database.Find(x => x.unitType == type);
    // }

    public UnitData Get(string name)
    {
        foreach (UnitData item in database)
        {
            if (item.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }

        return new UnitData();
    }
}