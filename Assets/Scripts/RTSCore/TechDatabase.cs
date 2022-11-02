using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TechData : ScriptableObject
{
    public RTSTechType techType;
}

[CreateAssetMenu(fileName = "New Tech Database", menuName = "RTS/Tech/Database")]
public class TechDatabase : ScriptableObject
{
    [SerializeField] private List<TechData> database = new List<TechData>();

    public TechData Get(RTSTechType type)
    {
        return database.Find(x => x.techType == type);
    }

    public TechData Get(string name)
    {
        foreach (TechData item in database)
        {
            if (item.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }

        return new TechData();
    }
}