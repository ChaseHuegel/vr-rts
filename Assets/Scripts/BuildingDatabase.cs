using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Building Database", menuName = "RTS/Buildings/Database")]
public class BuildingDatabase : ScriptableObject
{
    [SerializeField] private List<BuildingElement> database = new List<BuildingElement>();

    public BuildingElement Get(string name)
    {
        foreach (BuildingElement item in database)
        {
            if (item.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }

        return new BuildingElement();
    }
}