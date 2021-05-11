using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class Structure : Obstacle, IFactioned
{
    public byte factionID = 0;
    private Faction faction;

    // Set to grab data about this building from database.
    public RTSBuildingType rtsBuildingType;
    protected RTSBuildingTypeData rtsBuildingTypeData;
    protected HealthBar buildingHealthBar;

    // Built signals that building construction has completed, it does
    // not signal whether a building needs repairs after having been
    // damaged.
    private bool built = false;
    private Damageable damageable;
    public Damageable AttributeHandler { get { return damageable; } }

    [Header( "Construction Phases" )]
    private GameObject constructionPhaseBeginPrefab;
    private GameObject constructionPhaseMiddlePrefab;
    private GameObject constructionPhaseEndPrefab;

    public Faction GetFaction() { return faction; }
    public void UpdateFaction() { faction = GameMaster.Factions.Find(x => x.index == factionID); }

    public bool NeedsRepairs() { return damageable.GetAttributePercent(Attributes.HEALTH) < 1f; }
    public bool IsBuilt() { return built; }

    public override void Initialize()
    {
        base.Initialize();

        // Retrieve building data from database.
        rtsBuildingTypeData = GameMaster.Instance.FindBuildingData(rtsBuildingType);

        UpdateFaction();

        damageable = GetComponent<Damageable>();
        if (!damageable)
            Debug.Log("No damageable component on structure!");

        // TODO: Could move this to be part of the RTSBuildingTypeData database and
        // pull the prefabs directly from their. Would simplify creation/addition of
        // new building types.
        SetConstructionPhasePrefabs();
        if (!constructionPhaseBeginPrefab || !constructionPhaseMiddlePrefab || !constructionPhaseEndPrefab)
            Debug.Log("Missing construction stage prefab(s).");

        if (buildingHealthBar = GetComponentInChildren<HealthBar>( true ))
        {
            buildingHealthBar.enabled = false;
            RefreshHealthBar();
        }
        else
            Debug.Log("No building health bar found.");
    }

    public void TryRepair(int count, Actor repairer = null)
    {
        AttributeHandler.Heal(count, AttributeChangeCause.HEALED, repairer.AttributeHandler);
        RefreshHealthBar();
    }

    void RefreshHealthBar()
    {
        buildingHealthBar.SetFilledAmount(damageable.GetAttributePercent(Attributes.HEALTH));

        // Only set display of building stages if construction hasn't been
        // completed yet.
        if (!built)
        {
            if (damageable.GetAttributePercent(Attributes.HEALTH) >= 1f)
            {
                constructionPhaseEndPrefab.SetActive(true);
                constructionPhaseMiddlePrefab.SetActive(false);
                constructionPhaseBeginPrefab.SetActive(false);
                AudioSource.PlayClipAtPoint(rtsBuildingTypeData.constructionCompletedAudio?.GetClip(), transform.position, 0.25f);
                built = true;

                PlayerManager.instance.IncreasePopulationLimit(rtsBuildingTypeData.populationSupported);
            }
            else if (damageable.GetAttributePercent(Attributes.HEALTH) >= 0.5f)
            {
                constructionPhaseEndPrefab.SetActive(false);
                constructionPhaseMiddlePrefab.SetActive(true);
                constructionPhaseBeginPrefab.SetActive(false);
            }
            else if (damageable.GetAttributePercent(Attributes.HEALTH) >= 0.0f)
            {
                constructionPhaseEndPrefab.SetActive(false);
                constructionPhaseMiddlePrefab.SetActive(false);
                constructionPhaseBeginPrefab.SetActive(true);
            }
        }
    }

    // Looks for 3 prefabs that are directly childed to the game object. The first
    // in the hierarchy is the end stage of construction, the second is the middle
    // stage, and the third is the beginning stage.
    void SetConstructionPhasePrefabs()
    {
        constructionPhaseEndPrefab = transform.GetChild(0).gameObject;
        constructionPhaseMiddlePrefab = transform.GetChild(1).gameObject;
        constructionPhaseBeginPrefab = transform.GetChild(2).gameObject;
    }

    public bool CanDropOff(ResourceGatheringType type)
    {
        return rtsBuildingTypeData.dropoffTypes.HasFlag(type);
    }
}
