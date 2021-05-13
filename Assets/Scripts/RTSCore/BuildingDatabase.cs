using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Building Database", menuName = "RTS/Buildings/Database")]
public class BuildingDatabase : ScriptableObject
{
    [SerializeField] private List<BuildingData> database = new List<BuildingData>();

    public BuildingData Get(RTSBuildingType type)
    {
        return database.Find(x => x.buildingType == type);
    }

    public BuildingData Get(string name)
    {
        foreach (BuildingData item in database)
        {
            if (item.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }

        return new BuildingData();
    }
}