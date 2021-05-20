using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum RTSUnitType
{
    None, 
    Drifter, // This is the default villager, they have multiple goals and wander between them.
    Builder, 
    Lumberjack,
    GoldMiner,
    StoneMiner,
    Farmer,
    Swordsman,
    
};

