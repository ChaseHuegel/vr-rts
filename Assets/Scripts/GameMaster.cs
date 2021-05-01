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
    public static BuildingElement GetBuilding(string name) { return Instance.buildingDatabase.Get(name); }
}