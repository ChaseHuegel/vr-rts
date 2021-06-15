using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Flags]
public enum ResourceGatheringType 
{
    None = 0,
    Grain = 1,
    Wood = 2,
    Stone = 4,
    Gold = 8,
    Berries = 16,
    Fish = 32,
    Meat = 64,
};
