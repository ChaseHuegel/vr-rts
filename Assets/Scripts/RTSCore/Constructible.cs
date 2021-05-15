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
    public GameObject OnBuiltPrefab;

    public GameObject[] ConstructionStages;
    private int currentStage;

    public override void Initialize()
    {
        base.Initialize();

        UpdateFaction();

        if (!(damageable = GetComponent<Damageable>()))
            Debug.Log("No damageable component on constructible!");

        damageable.OnHealthRegainEvent += OnBuild;

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
        int progressStage = (int)(progress / (1f / ConstructionStages.Length));

        if (currentStage != progressStage)
        {
            ConstructionStages[currentStage].SetActive(false);
            ConstructionStages[progressStage].SetActive(true);

            currentStage = progressStage;
        }
    }

    public void TryBuild(int count, Actor builder = null)
    {
        AttributeHandler.Heal(count, AttributeChangeCause.HEALED, builder.AttributeHandler);
    }

    public void OnBuild(object sender, Damageable.HealthRegainEvent e)
    {
        if (e.cause != AttributeChangeCause.HEALED)
            return;

        if (AttributeHandler.GetAttribute(Attributes.HEALTH).GetValue() + e.amount >= AttributeHandler.GetAttribute(Attributes.HEALTH).GetMax())
        {
            //  Try placing a prefab
            if (OnBuiltPrefab != null)
            {
                Instantiate(OnBuiltPrefab, transform.position, transform.rotation);
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
