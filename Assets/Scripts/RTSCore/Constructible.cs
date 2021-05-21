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

    public byte factionID = 0;
    private Faction faction;
    public Faction GetFaction() { return faction; }
    public void UpdateFaction() { faction = GameMaster.Factions.Find(x => x.index == factionID); }

    public bool DestroyOnBuilt = true;
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

        damageable.OnHealthRegainEvent += OnBuild;

        if (!(audioSource = GetComponent<AudioSource>()))
            Debug.Log("Audiosource component missing.", this);

        ResetStages();
    }

    public bool IsBuilt()
    {
        return AttributeHandler.GetAttributePercent(Attributes.HEALTH) >= 1f;
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
                GameObject obj = Instantiate(OnBuiltPrefab, transform.position, transform.rotation);
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
