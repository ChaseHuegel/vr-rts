using System;

[Serializable]
public enum RTSUnitType
{
    None,
    Villager,
    Swordsman,
    OrcGrunt,
    Scout,
    Clubman,
}

[Serializable]
public enum RTSTechType
{
    // Stone Age
    None,
    Villager,
    Clubman,
    FishingBoat,
    TradeBoat,

    // Tool Age
    Bowman,
    AxemanResearch,
    Axeman,
    CavalryScout,
    LightTransport,
    ScoutShip,
    SmallWallResearch,
    SmallWall,
    WatchTowerResearch,
    WatchTower,
    InfantryCavalryAttack1Research,
    InfantryArmor2Research,
    CavalryArmor2Research,
    ArcherArmor2Research
}