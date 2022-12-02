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
    public static BuildingData GetBuilding(BuildingType type) { return Instance.buildingDatabase.Get(type); }
    public static BuildingData GetBuilding(string name) { return Instance.buildingDatabase.Get(name); }

    public UnitDatabase unitDatabase;
    public static UnitDatabase GetUnitDatabase() { return Instance.unitDatabase; }
    // public static UnitData GetUnit(RTSUnitType type)
    // {
    //      return Instance.unitDatabase.Get(type);
    // }

    public static UnitData GetUnit(string name) { return Instance.unitDatabase.Get(name); }

    [Header("Factions")]
    public List<Faction> factions;
    public static List<Faction> Factions { get { return Instance.factions;} }
    
    [Header("Misc. Prefabs")]
    public GameObject floatingIndicatorPrefab;
    public GameObject interactionPanelResourceCostPrefab;
    public GameObject worldButtonHintPrefab;

    [Header("FX")]
    public GameObject buildingDamagedFX;
    public GameObject buildingDestroyedFX;
    public GameObject buildingPlacementDeniedFX;

    [Header("Sounds")]
    public AudioClip buildingDestroyedSound;
    public AudioClip buildingPlacementAllowedSound;
    public AudioClip buildingPlacementDeniedSound;
    public AudioClip setRallyPointSound;
    public AudioClip queueSuccessSound;
    public AudioClip queueFailedSound;
    public AudioClip dequeueSound;
    public AudioClip teleportSound;
    public AudioClip epochResearchCompleteSound;
    
    [Header("Building Health Bars")]
    public Sprite healthBarBackground;
    public Color healthBarBackgroundColor = Color.black;
    public Sprite healthBarForeground;
    public Color healthBarForegroundColor = Color.red;
    public Color healthBarTextColor = Color.white;

    [Header("Queue Menu Progress Bar Settings")]
    public Sprite progressImage;
    public Color progressColor;
    public TMPro.TMP_FontAsset progressFont;

    [Header("Queue Menu Button Settings")]
    public GameObject buttonBasePrefab;
    public GameObject buttonMovingPartPrefab;
    public Material buttonBaseMaterial;
    public Material cancelButtonMaterial;
    public AudioClip onQueueButtonDownSound;
    public AudioClip onQueueButtonUpSound;

    [Header("Queue Menu Queue Slot Settings")]
    public Sprite emptyQueueSlotSprite;    
    
    [Header("Unit settings")]
    public int maximumUnitSelectionCount = 20;
    public float unitCorpseDecayTime = 30.0f;

    public static void SendFloatingIndicator(Vector3 pos, string text, Color color)
    {
        GameObject obj = Instantiate(Instance.floatingIndicatorPrefab, pos, Quaternion.identity);
        obj.GetComponent<TextMeshPro>().text = text;
        obj.GetComponent<TextMeshPro>().color = color;
    }
}
