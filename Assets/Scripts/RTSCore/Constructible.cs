using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

public class Constructible : Obstacle
{
    [Tooltip("Destroy the constructible gameobject once construction is complete.")]
    public bool DestroyOnBuilt = true;
    public bool ClearExistingWalls;
    public BuildingData buildingData;
    public GameObject OnBuiltPrefab;
    public GameObject[] ConstructionStages;
    private int currentStage;

    public override void Initialize()
    {
        base.Initialize();

        Attributes.AddOrUpdate(AttributeConstants.HEALTH, 0f, buildingData.maximumHitPoints);
        OnHealthRegainEvent += OnBuild;

        ResetStages();

        if (ClearExistingWalls)
        {
            UnbakeFromGrid();

            // TODO: Switch to rotated/fixed prefab?
            // Stretch if diagonal gate
            if (buildingData.buildingType == BuildingType.WallGate)
            {
                float angle = Mathf.Round(transform.eulerAngles.y);
                if (angle == 45.0f || angle == 135.0f || angle == 225.0f || angle == 315.0f)
                    transform.localScale += new Vector3(0.0f, 0.01199419f, 0.0f);
            }

            RemoveExistingWalls();
            BakeToGrid();
        }
    }

    protected override void UpdateSkin()
    {
        if (SkinRendererTargets.Length <= 0) return;

        if (Faction?.skin?.buildingMaterial)
        {
            foreach (var renderer in SkinRendererTargets)
                renderer.sharedMaterial = Faction.skin.buildingMaterial;
        }
    }

    private void RemoveExistingWalls()
    {
        Cell thisCell = GetCell();
        Cell[] neighbors = thisCell.neighbors().ToArray();

        for (int i = 0; i < neighbors.Length; ++i)
        {
            WallSegment wallSegment = neighbors[i].GetFirstOccupant<Body>()?.GetComponent<WallSegment>();

            if (wallSegment)
                Destroy(wallSegment.gameObject);
        }

        WallSegment thisWallSegment = thisCell.GetFirstOccupant<Body>()?.GetComponent<WallSegment>();

        if (thisWallSegment)
            Destroy(thisWallSegment.gameObject);
    }

    public override void FetchBoundingDimensions()
    {
        base.FetchBoundingDimensions();

        BoundingDimensions.x = buildingData.boundingDimensionX;
        BoundingDimensions.y = buildingData.boundingDimensionY;
    }

    public bool IsBuilt()
    {
        return Attributes.Get(AttributeConstants.HEALTH).IsMax();
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
        float progress = Attributes.CalculatePercentOf(AttributeConstants.HEALTH);
        int progressStage = 0;// = (int)(progress / (1f / ConstructionStages.Length));

        if (progress >= 0.33)
            progressStage = 1;
        else if (progress >= 0.66f)
            progressStage = 2;

        if (currentStage != progressStage)
        {
            ConstructionStages[currentStage].SetActive(false);
            ConstructionStages[progressStage].SetActive(true);

            currentStage = progressStage;
        }
    }

    public void OnBuild(object sender, HealthRegainEvent e)
    {
        //Debug.Log(Attributes.ValueOf(AttributeConstants.HEALTH) + "/" + Attributes.MaxValueOf(AttributeConstants.HEALTH));
        if (Attributes.Get(AttributeConstants.HEALTH).PeekAdd(e.amount) == GetMaxHealth())
        {
            if (DestroyOnBuilt)
            {
                UnbakeFromGrid();
                Destroy(gameObject);
            }

            //  Try placing a prefab
            if (OnBuiltPrefab != null)
            {
                GameObject gameObject = Instantiate(OnBuiltPrefab, transform.position, transform.rotation);
                Structure structure = gameObject.GetComponent<Structure>();
                if (structure)
                    structure.Faction = this.Faction;
                else
                {
                    Resource constructible = gameObject.GetComponent<Resource>();
                    constructible.Faction = this.Faction;
                }

                AudioSource.PlayClipAtPoint(buildingData.constructionCompletedAudio?.GetClip(), transform.position);
            }
        }
        else
        {
            UpdateStage();
        }
    }
}
