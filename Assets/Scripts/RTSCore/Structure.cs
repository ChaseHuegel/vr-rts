using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

public class Structure : Obstacle
{
    public readonly static List<Structure> AllStructures = new();
    public static float fireFxStart = 0.35f;
    public BuildingData buildingData;   
    private AudioSource audioSource;
    private GameObject buildingDamagedFX;
    private ParticleSystem smokeParticleSystem;
    ParticleSystem.MainModule psMain;
    private GameObject fireGlowParticleSystem;
    private GameObject flamesParticleSystem;
    private GameObject sparksParticleSystem;
    public bool NeedsRepairs() => Attributes.Get(AttributeType.HEALTH).IsMax();

    public void Awake()
    {
        if (!(audioSource = GetComponent<AudioSource>()))
            Debug.Log("Audiosource component not found.");
    }

    protected override void UpdateSkin()
    {
        if (SkinRendererTargets == null || SkinRendererTargets.Length <= 0) return;

        if (Faction?.skin?.buildingMaterial)
        {
            foreach (var renderer in SkinRendererTargets)
                renderer.sharedMaterial = Faction.skin.buildingMaterial;
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        AllStructures.Add(this);

        // Setup some defaults that tend to get switched in the editor.
        IgnorePanning ignorePanning = GetComponentInChildren<IgnorePanning>();
        if (ignorePanning)
            ignorePanning.gameObject.layer = LayerMask.NameToLayer("UI");

        if (!buildingData)
            Debug.LogError("BuildingData not set.");


        // Set max health based on building database hit point value.
        Attributes.Get(AttributeType.HEALTH).MaxValue = buildingData.maximumHitPoints;

        HookIntoEvents();

        // Only refresh visuals if hit points are not full so we don't generate
        // building damage FX particle systems on buildings that don't need them yet.
        // We can generate them at startup later on to gain real time performance
        // if needed.
        if (!Attributes.Get(AttributeType.HEALTH).IsMax())
            RefreshVisuals();
    }

    private void HookIntoEvents()
    {
        OnDamageEvent += OnDamage;
        Damageable.OnDeathEvent += OnDeath;
    }

    private void CleanupEvents()
    {
        OnDamageEvent -= OnDamage;
        Damageable.OnDeathEvent -= OnDeath;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        CleanupEvents();
        AllStructures.Remove(this);
    }

    public override void FetchBoundingDimensions()
    {
        base.FetchBoundingDimensions();

        BoundingDimensions.x = buildingData.boundingDimensionX;
        BoundingDimensions.y = buildingData.boundingDimensionY;
    }

    protected void CreateBuildingDamageFX()
    {
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
    }

    void OnDamage(object sender, Damageable.DamageEvent e)
    {
        RefreshVisuals();
    }

    private void OnDeath(object sender, Damageable.DeathEvent e)
    {
        if (e.victim == this)
        {
            AudioSource.PlayClipAtPoint(GameMaster.GetAudio("building_collapsed").GetClip(), transform.position, 0.5f);
            UnbakeFromGrid();
            Destroy(gameObject);
        }
    }

    public void TryRepair(float count, ActorV2 repairer = null)
    {
        Heal(count, AttributeChangeCause.HEALED, repairer);
        RefreshVisuals();
    }

    void RefreshVisuals()
    {
        if (!buildingDamagedFX)
            CreateBuildingDamageFX();

        float healthPercent = Attributes.CalculatePercentOf(AttributeType.HEALTH);
        if (healthPercent == 1f)
        {
            if (buildingDamagedFX.activeSelf)
                buildingDamagedFX.SetActive(false);

            return;
        }

        // Start damage fx
        else if (buildingDamagedFX.activeInHierarchy == false)
        {
            smokeParticleSystem.gameObject.SetActive(true);
            buildingDamagedFX.SetActive(true);
        }
        else if (buildingDamagedFX.activeInHierarchy == true)
        {
            var emission = smokeParticleSystem.emission;

            // Base rate desired + percent health missing * modifier.
            emission.rateOverTime = 4.0f + ((1.0f - healthPercent) * 30);

            float modifier = 2.0f - healthPercent;
            psMain.startLifetime = new ParticleSystem.MinMaxCurve(2.0f + modifier, 3.0f + modifier);

            modifier = (1.0f - healthPercent) * 0.75f;
            psMain.startSize = new ParticleSystem.MinMaxCurve(0.0f + modifier, 0.5f + modifier);
            
            if (healthPercent <= fireFxStart)
            {
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = GameMaster.GetAudio("crackling_fire").GetClip();
                    audioSource.volume = 0.25f;
                    audioSource.Play();
                }
                StartFireFX();
            }
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            StopFireFX();            
        }
    }

    private void StartFireFX()
    {
        fireGlowParticleSystem.SetActive(true);
        flamesParticleSystem.SetActive(true);
        sparksParticleSystem.SetActive(true);
    }

    private void StopFireFX()
    {
        fireGlowParticleSystem.SetActive(false);
        flamesParticleSystem.SetActive(false);
        sparksParticleSystem.SetActive(false);
    }

    public bool CanDropOff(ResourceGatheringType type)
    {
        return buildingData.dropoffTypes.HasFlag(type);
    }
}
