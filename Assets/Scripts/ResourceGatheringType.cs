using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Flags]
public enum ResourceGatheringType {
    None = 0,
    Grain = 1,
    Wood = 2,
    Stone = 4,
    Gold = 8,
};
