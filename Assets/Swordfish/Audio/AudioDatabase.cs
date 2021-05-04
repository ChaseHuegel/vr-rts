using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Audio
{

[CreateAssetMenu(fileName = "New Audio Database", menuName = "Swordfish/Audio/Database")]
public class AudioDatabase : ScriptableObject
{
    [SerializeField] private List<SoundElement> database = new List<SoundElement>();

    public SoundElement Get(string name)
    {
        foreach (SoundElement item in database)
        {
            if (item.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
            {
                return item;
            }
        }

        return CreateInstance<SoundElement>();
    }
}

}