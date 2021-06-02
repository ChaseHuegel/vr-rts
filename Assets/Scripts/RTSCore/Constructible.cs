using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

[RequireComponent(typeof(Damageable))]
public class Constructible : Obstacle, IFactioned
{
    private Damageable damageable;
    public Damageable AttributeHandler { get { return damageable; } }

    private Faction faction;
    public Faction GetFaction() { return faction; }
    public void UpdateFaction() { faction = GameMaster.Factions.Find(x => x.index == factionId); }

    public bool DestroyOnBuilt = true;
    public bool ClearExistingWalls;
    public BuildingData buildingData;
    public GameObject OnBuiltPrefab;
    public GameObject[] ConstructionStages;
    private int currentStage;
    private AudioSource audioSource;


    public override void Initialize()
    {
        base.Initialize();
        UpdateFaction();

        if (!(damageable = GetComponent<Damageable>()))
            Debug.Log("No damageable component on constructible!");

        // damageable.GetAttribute(Attributes.HEALTH).SetValue(0);
        damageable.GetAttribute(Attributes.HEALTH).SetMax(buildingData.hitPoints);
        damageable.OnHealthRegainEvent += OnBuild;

        if (!(audioSource = GetComponent<AudioSource>()))
            Debug.Log("Audiosource component missing.", this);

        ResetStages();

        if (ClearExistingWalls)
        {
            UnbakeFromGrid();

            // TODO: Switch to rotated/fixed prefab?
            // Stretch if diagonal gate
            if (buildingData.buildingType == RTSBuildingType.Wood_Wall_Gate ||
                buildingData.buildingType == RTSBuildingType.Stone_Wall_Gate)
            {                    
                float angle = Mathf.Round(transform.eulerAngles.y);
                if (angle == 45.0f || angle == 135.0f || angle == 225.0f || angle == 315.0f)
                    transform.localScale += new Vector3(0.0f, 0.01199419f, 0.0f);
            }

            RemoveExistingWalls();
            
            BakeToGrid();
        }
    }

    private void RemoveExistingWalls()
    {
        Cell thisCell = GetCellAtGrid();
        Cell[] neighbors = thisCell.neighbors().ToArray();

        for (int i = 0; i < neighbors.Length; ++i)
        {
            WallSegment wallSegment = neighbors[i].GetFirstOccupant<Structure>()?.GetComponent<WallSegment>();
            
            if (wallSegment)
                Destroy(wallSegment.gameObject);            
        }

        WallSegment thisWallSegment = thisCell.GetFirstOccupant<Structure>()?.GetComponent<WallSegment>();

        if (thisWallSegment)
            Destroy(thisWallSegment.gameObject);
    }

    public override void FetchBoundingDimensions()
    {
        base.FetchBoundingDimensions();

        boundingDimensions.x = buildingData.boundingDimensionX;
        boundingDimensions.y = buildingData.boundingDimensionY;
    }

    public bool IsBuilt()
    {
        return AttributeHandler.GetAttributePercent(Attributes.HEALTH) >= 1.0f;
    }

    private void ResetStages()
    {
        foreach (GameObject obj in ConstructionStages)
            obj.SetActive(false);

        currentStage = 0;
        ConstructionStages[currentStage].SetActive(true);
    }

    private void UpdateStage()
    {        
        float progress = AttributeHandler.GetAttributePercent(Attributes.HEALTH);   
        int progressStage = 0;// = (int)(progress / (1f / ConstructionStages.Length));
        
        if (progress >= 0.45f)
            progressStage = 1;
        else if (progress >= 0.95f)
            progressStage = 2;

        if (currentStage != progressStage)
        {
            ConstructionStages[currentStage].SetActive(false);
            ConstructionStages[progressStage].SetActive(true);

            currentStage = progressStage;
        }
    }

    public void TryBuild(float count, Actor builder = null)
    {
        AttributeHandler.Heal(count, AttributeChangeCause.HEALED, builder.AttributeHandler);
    }

    public void OnBuild(object sender, Damageable.HealthRegainEvent e)
    {
        if (e.cause != AttributeChangeCause.HEALED)
            return;

        if (e.health >= AttributeHandler.GetMaxHealth())
        {
            //  Try placing a prefab
            if (OnBuiltPrefab != null)
            {
                Instantiate(OnBuiltPrefab, transform.position, transform.rotation);                
                AudioSource.PlayClipAtPoint(buildingData.constructionCompletedAudio?.GetClip(), transform.position);
            }

            if (DestroyOnBuilt)
            {
                UnbakeFromGrid();
                Destroy(this.gameObject);
            }
        }
        else
        {
            UpdateStage();
        }
    }
}
