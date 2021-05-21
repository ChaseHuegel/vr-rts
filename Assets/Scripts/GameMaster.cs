using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Audio;
using TMPro;

public class GameMaster : Singleton<GameMaster>
{
    [Header("Databases")]
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

    public UnitDatabase unitDatabase;
    public static UnitDatabase GetUnitDatabase() { return Instance.unitDatabase; }
    public static UnitData GetUnit(RTSUnitType type)
    {
         return Instance.unitDatabase.Get(type);
    }

    public static UnitData GetUnit(string name) { return Instance.unitDatabase.Get(name); }

    [Header("Factions")]
    public List<Faction> factions;
    public static List<Faction> Factions { get { return Instance.factions;} }

    [Header("Prefabs")]
    public GameObject floatingIndicatorPrefab;

    public static void SendFloatingIndicator(Vector3 pos, string text, Color color)
    {
        GameObject obj = Instantiate(Instance.floatingIndicatorPrefab, pos, Quaternion.identity);
        obj.GetComponent<TextMeshPro>().text = text;
        obj.GetComponent<TextMeshPro>().color = color;
    }
}
