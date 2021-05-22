using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

[RequireComponent(typeof(Damageable))]
public class Structure : Obstacle, IFactioned
{
    public byte factionID = 0;
    private Faction faction;
    public BuildingData buildingData;
    private Damageable damageable;
    public Damageable AttributeHandler { get { return damageable; } }
    private AudioSource audioSource;
    private GameObject buildingDamagedFX;
    private ParticleSystem smokeParticleSystem;
    ParticleSystem.MainModule psMain;
    private GameObject fireGlowParticleSystem;
    private GameObject flamesParticleSystem;
    private GameObject sparksParticleSystem;
    public Faction GetFaction() { return faction; }

    public void UpdateFaction() { faction = GameMaster.Factions.Find(x => x.index == factionID); }

    public bool NeedsRepairs() { return damageable.GetHealthPercent() < 1f; }

    public void Awake()
    {
        if (!(audioSource = GetComponent<AudioSource>()))
            Debug.Log("Audiosource component not found.");
    }

    public override void Initialize()
    {
        base.Initialize();

        // Setup some defaults that tend to get switched in the editor.
        IgnorePanning ignorePanning = GetComponentInChildren<IgnorePanning>();
        if (ignorePanning)
            ignorePanning.gameObject.layer = LayerMask.NameToLayer("UI");

        if (!buildingData)
            Debug.Log("BuildingData not set.");

        UpdateFaction();

        if (!(damageable = GetComponent<Damageable>()))
            Debug.Log("No damageable component on structure!");

        // Set max health based on building database hit point value.
        damageable.GetAttribute(Attributes.HEALTH).SetMax(buildingData.hitPoints);
        damageable.OnDamageEvent += OnDamage;

        if (!GameMaster.Instance.buildingDamagedFX)
            Debug.Log("buildingDamagedFX not set in GameMaster.", this);

        buildingDamagedFX = Instantiate(GameMaster.Instance.buildingDamagedFX, transform.position, Quaternion.identity, transform);
        foreach (ParticleSystem pSystem in buildingDamagedFX.transform.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (pSystem.name == "Smoke_A")
            {
                smokeParticleSystem = pSystem;
                psMain = smokeParticleSystem.main;
            }

            else if (pSystem.name == "Fire_Glow")
                fireGlowParticleSystem = pSystem.gameObject;

            else if (pSystem.name == "sparks")
                sparksParticleSystem = pSystem.gameObject;

            else if (pSystem.name == "Flames")
                flamesParticleSystem = pSystem.gameObject;
        }

        if (buildingData.populationSupported > 0)
            PlayerManager.instance.IncreasePopulationLimit(buildingData.populationSupported);

        RefreshVisuals();
    }

    void OnDamage(object sender, Damageable.DamageEvent e)
    {
        RefreshVisuals();

        if (AttributeHandler.GetAttributePercent(Attributes.HEALTH) <= 0.0f)
        {
            AudioSource.PlayClipAtPoint(GameMaster.GetAudio("building_collapsed").GetClip(), transform.position, 0.5f);
            Destroy(this.gameObject);
        }
    }

    public void TryRepair(float count, Actor repairer = null)
    {
        AttributeHandler.Heal(count, AttributeChangeCause.HEALED, repairer.AttributeHandler);
        RefreshVisuals();
    }

    void RefreshVisuals()
    {        
        if (!buildingDamagedFX)
            return;

        float healthPercent = damageable.GetAttributePercent(Attributes.HEALTH);
        if (healthPercent >= 1.0f)
        {
            if (buildingDamagedFX.activeSelf) buildingDamagedFX.SetActive(false);            
            return;
        }

        var emission = smokeParticleSystem.emission;
        emission.rateOverTime = (1.0f - healthPercent) * 30;
 
        float modifier = 2.0f - healthPercent;  
        psMain.startLifetime = new ParticleSystem.MinMaxCurve(2.0f + modifier, 3.0f + modifier);

        modifier = (1.0f - healthPercent) * 0.75f;
        psMain.startSize = new ParticleSystem.MinMaxCurve(0.0f + modifier, 0.5f + modifier);

        if (damageable.GetAttributePercent(Attributes.HEALTH) <= 0.35f)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = GameMaster.GetAudio("crackling_fire").GetClip();
                audioSource.volume = 0.25f;
                audioSource.Play();
            }

            if (!fireGlowParticleSystem.activeSelf) fireGlowParticleSystem.SetActive(true);
            if (!flamesParticleSystem.activeSelf) flamesParticleSystem.SetActive(true);

            if (sparksParticleSystem.activeSelf) sparksParticleSystem.SetActive(false);            
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            if (fireGlowParticleSystem.activeSelf) fireGlowParticleSystem.SetActive(false);
            if (flamesParticleSystem.activeSelf) flamesParticleSystem.SetActive(false);            
            if (sparksParticleSystem.activeSelf) sparksParticleSystem.SetActive(false);
        }

        if (!smokeParticleSystem.gameObject.activeSelf) smokeParticleSystem.gameObject.SetActive(true);
        if (!buildingDamagedFX.activeSelf) buildingDamagedFX.SetActive(true);
    }

    public bool CanDropOff(ResourceGatheringType type)
    {
        return buildingData.dropoffTypes.HasFlag(type);
    }
}
