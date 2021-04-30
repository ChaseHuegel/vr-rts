using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Resource Node Database", menuName = "RTS/Resources/Database")]
public class ResourceNodeDatabase : ScriptableObject
{
    [SerializeField] private List<ResourceElement> database = new List<ResourceElement>();

    public ResourceElement Get(string name)
    {
        foreach (ResourceElement item in database)
        {
            if (item.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }

        return new ResourceElement();
    }
}