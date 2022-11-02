using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

public class Structure : Obstacle
{
    public readonly static List<Structure> AllStructures = new();

    public BuildingData buildingData;

    [Header("Skin Settings")]
    public MeshRenderer[] meshes;
    public SkinnedMeshRenderer[] skinnedMeshes;

    private Damageable damageable;
    public Damageable AttributeHandler { get { return damageable; } }
    private AudioSource audioSource;
    private GameObject buildingDamagedFX;
    private ParticleSystem smokeParticleSystem;
    ParticleSystem.MainModule psMain;
    private GameObject fireGlowParticleSystem;
    private GameObject flamesParticleSystem;
    private GameObject sparksParticleSystem;
    private PlayerManager playerManager;

    public bool NeedsRepairs() => !damageable.Attributes.Get(AttributeConstants.HEALTH).IsMax();

    public void Awake()
    {
        if (!(audioSource = GetComponent<AudioSource>()))
            Debug.Log("Audiosource component not found.");
    }

    public override void Initialize()
    {
        base.Initialize();

        AllStructures.Add(this);

        // TODO: Will need checks here for multiplayer
        playerManager = PlayerManager.instance;

        

        // Setup some defaults that tend to get switched in the editor.
        IgnorePanning ignorePanning = GetComponentInChildren<IgnorePanning>();
        if (ignorePanning)
            ignorePanning.gameObject.layer = LayerMask.NameToLayer("UI");

        if (!buildingData)
            Debug.Log("BuildingData not set.");

        if (!(damageable = GetComponent<Damageable>()))
            Debug.Log("No damageable component on structure!");

        // Set max health based on building database hit point value.
        damageable.Attributes.Get(AttributeConstants.HEALTH).MaxValue = buildingData.hitPoints;
        damageable.OnDamageEvent += OnDamage;

        if (!GameMaster.Instance.buildingDamagedFX)
            Debug.Log("buildingDamagedFX not set in GameMaster.", this);

        // Only refresh visuals if hit points are not full so we don't generate
        // building damage FX particle systems on buildings that don't need them yet.
        // We can generate them at startup later on to gain real time performance
        // if needed.
        if (!damageable.Attributes.Get(AttributeConstants.HEALTH).IsMax())
            RefreshVisuals();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
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

        if (AttributeHandler.Attributes.ValueOf(AttributeConstants.HEALTH) == 0f)
        {
            AudioSource.PlayClipAtPoint(GameMaster.GetAudio("building_collapsed").GetClip(), transform.position, 0.5f);
            UnbakeFromGrid();
            Destroy(gameObject);
        }
    }

    public void TryRepair(float count, ActorV2 repairer = null)
    {
        AttributeHandler.Heal(count, AttributeChangeCause.HEALED, repairer);
        RefreshVisuals();
    }

    void RefreshVisuals()
    {
        if (!buildingDamagedFX)
            CreateBuildingDamageFX();

        float healthPercent = damageable.Attributes.CalculatePercentOf(AttributeConstants.HEALTH);
        if (healthPercent == 1f)
        {
            if (buildingDamagedFX.activeSelf)
                buildingDamagedFX.SetActive(false);

            return;
        }

        var emission = smokeParticleSystem.emission;

        // Base rate desired + percent health missing * modifier.
        emission.rateOverTime = 4.0f + ((1.0f - healthPercent) * 30);

        float modifier = 2.0f - healthPercent;
        psMain.startLifetime = new ParticleSystem.MinMaxCurve(2.0f + modifier, 3.0f + modifier);

        modifier = (1.0f - healthPercent) * 0.75f;
        psMain.startSize = new ParticleSystem.MinMaxCurve(0.0f + modifier, 0.5f + modifier);

        if (healthPercent <= 0.35f)
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
