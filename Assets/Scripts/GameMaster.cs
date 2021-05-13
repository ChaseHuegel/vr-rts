using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Audio;


public class GameMaster : Singleton<GameMaster>
{
    public AudioDatabase audioDatabase;
    public static AudioDatabase GetAudioDatabase() { return Instance.audioDatabase; }
    public static SoundElement GetAudio(string name) { return Instance.audioDatabase.Get(name); }

    public ResourceNodeDatabase nodeDatabase;
    public static ResourceNodeDatabase GetNodeDatabase() { return Instance.nodeDatabase; }
    public static ResourceElement GetNode(string name) { return Instance.nodeDatabase.Get(name); }

    public BuildingDatabase buildingDatabase;
    public static BuildingDatabase GetBuildingDatabase() { return Instance.buildingDatabase; }
    public static BuildingData GetBuilding(RTSBuildingType type) { return Instance.buildingDatabase.Get(type); }
    public static BuildingData GetBuilding(string name) { return Instance.buildingDatabase.Get(name); }

    public List<Faction> factions;
    public static List<Faction> Factions { get { return Instance.factions;} }

    public List<RTSUnitTypeData> rtsUnitDataList = new List<RTSUnitTypeData>();

    public RTSUnitTypeData FindUnitData(RTSUnitType type)
    {
        RTSUnitTypeData ret = rtsUnitDataList.Find(x => x.unitType == type );
        return ret;
    }
}
