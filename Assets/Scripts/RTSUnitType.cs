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
    TownHall,
    Barracks,
    Dock,
    Granary,
    StoragePit,
    Villager,
    Clubman,
    FishingBoat,
    TradeBoat,
    ToolAgeResearch,

    // Tool Age
    Bowman,
    AxemanResearch,
    Axeman,
    CavalryScout,
    LightTransport,
    ScoutShip,
    SmallWallResearch,
    SmallWall,
    CivilCenter,
    WatchTowerResearch,
    WatchTower,
    InfantryCavalryAttack1Research,
    InfantryArmor2Research,
    CavalryArmor2Research,
    ArcherArmor2Research
}