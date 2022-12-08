using Swordfish;
using Swordfish.Audio;
using Swordfish.Library.Types;
using Swordfish.Navigation;
using UnityEngine;

public abstract class UnitV2 : ActorV2
{
    public abstract bool IsCivilian { get; }
    public GameObject damagedFxPrefab;
    public GameObject deathFxPrefab;
    public GameObject projectilePrefab;
    public Transform projectileOrigin;
    [Tooltip("Sound played when unit heals or repairs a target.")]
    public SoundElement healSound;
    protected AudioClip healClip;
    [Tooltip("Collection of sounds chosen from when the unit attacks a target.")]
    public SoundElement attackSound;
    protected AudioClip attackClip;
    public UnitData unitData;
    
    private bool deathFxStarted;
    public bool AttackingTarget
    {
        get => AttackingTargetBinding.Get();
        set => AttackingTargetBinding.Set(value);
    }

    public bool HealingTarget
    {
        get => HealTargetBinding.Get();
        set => HealTargetBinding.Set(value);
    }

    public DataBinding<bool> AttackingTargetBinding { get; private set; } = new();
    public DataBinding<bool> HealTargetBinding { get; private set; } = new();

    protected float AttackTimer;
    protected float HealTimer;

    private Vector3 originalScale = Vector3.one;

    protected override void Start()
    {        
        base.Start();
        OnLoadUnitData(unitData);

        audioSource = GetComponent<AudioSource>();
        attackClip = attackSound?.GetClip();
        healClip = healSound?.GetClip();

        TryFetchRenderers();

        originalScale = gameObject.transform.localScale;
    }

    protected override void Update()
    {
        base.Update();
        if (!Frozen)
        {
            ProcessAttackRoutine(Time.deltaTime);
            ProcessHealRoutine(Time.deltaTime);
        }
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeType.DAMAGE, 1f);
        Attributes.AddOrUpdate(AttributeType.ATTACK_SPEED, 1f);
        Attributes.AddOrUpdate(AttributeType.ATTACK_RANGE, Attributes.ValueOf(AttributeType.REACH));
        Attributes.AddOrUpdate(AttributeType.HEAL_RATE, 1f);       
    }

    protected virtual void OnLoadUnitData(UnitData data)
    {
        Attributes.Get(AttributeType.ATTACK_RANGE).Value = data.attackRange;
        Attributes.Get(AttributeType.ATTACK_SPEED).Value = data.attackSpeed;
        Attributes.Get(AttributeType.DAMAGE).Value = data.attackDamage;
        Attributes.Get(AttributeType.HEALTH).MaxValue = data.maximumHitPoints;
        Attributes.Get(AttributeType.HEALTH).Value = data.maximumHitPoints;
        Attributes.Get(AttributeType.SPEED).MaxValue = data.movementSpeed;
        Attributes.Get(AttributeType.SPEED).Value = data.movementSpeed;
        Attributes.Get(AttributeType.HEAL_RATE).Value = data.healRate;
        Attributes.Get(AttributeType.HEAL_RATE).MaxValue = data.healRate;

        if (PlayerManager.AllUnitAttributeBonuses.TryGetValue(data, out var bonuses))
            foreach (StatUpgradeContainer bonus in bonuses)
                Attributes.Get(bonus.targetAttribute).AddModifier(bonus.targetAttribute, bonus.modifier, bonus.amount);
    }
    
    private bool TryFetchRenderers()
    {
        if (SkinRendererTargets.Length <= 0)
        {
            Renderer[] skinnedMeshRenderers = GetComponentsInChildren<Renderer>(false);
            if (skinnedMeshRenderers.Length <= 0) 
                return false;

            SkinRendererTargets = new Renderer[skinnedMeshRenderers.Length];
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
                SkinRendererTargets[i] = skinnedMeshRenderers[i];

            return true;
        }

        return false;
    }

    protected virtual void OnAttributeBonusChanged(object sender, PlayerManager.UnitAttributeBonusChangeEvent e)
    {
        if (e.unitData == unitData)
        {
            var attribute = Attributes.Get(e.bonus.targetAttribute);

            if (attribute?.MaxValue < int.MaxValue)
                attribute.AddMaxModifier(e.bonus.targetAttribute, e.bonus.modifier, e.bonus.amount);

            attribute.AddModifier(e.bonus.targetAttribute, e.bonus.modifier, e.bonus.amount);
        }
    }

    protected override void AttachListeners()
    {
        base.AttachListeners();
        PlayerManager.OnUnitAttributeBonusChangedEvent += OnAttributeBonusChanged;
    }

    protected override void CleanupListeners()
    {
        base.CleanupListeners();
        PlayerManager.OnUnitAttributeBonusChangedEvent -= OnAttributeBonusChanged;
    }

    protected override void OnFallingOntoBody(Body body)
    {
        base.OnFallingOntoBody(body);
        IssueTargetedOrder(body);
    }

    protected override void OnHealthRegain(HealthRegainEvent e)
    {
        base.OnHealthRegain(e);
        if (!currentHealFx)
            currentHealFx = Instantiate(GameMaster.Instance.onUnitHealedFxPrefab, transform.position, GameMaster.Instance.onUnitHealedFxPrefab.transform.rotation, transform);
    }

    protected override void OnDeath(DeathEvent e)
    {
        base.OnDeath(e);

        Frozen = true;
        CurrentPath = null;
        Order = UnitOrder.None;
        Destination = null;
        Target = null;

        ActorAnimationState deathState = Random.Range(1, 100) < 50 ? ActorAnimationState.DYING : ActorAnimationState.DYING2;

        SetAnimatorsInteger("ActorAnimationState", (int)deathState);

        AudioSource.PlayOneShot(GameMaster.GetAudio("unit_death").GetClip());
        if (deathFxPrefab && !deathFxStarted)
            Instantiate(deathFxPrefab, transform.position, deathFxPrefab.transform.rotation);

        deathFxStarted = true;

        Destroy(gameObject, GameMaster.Instance.unitCorpseDecayTime);
    }

    protected override void OnHeldChanged(object sender, DataChangedEventArgs<bool> e)
    {
        base.OnHeldChanged(sender, e);

        if (e.NewValue == true)
        {
            if (Faction.IsSameFaction(PlayerManager.Instance.faction))
                AudioSource.PlayOneShot(GameMaster.GetAudio("unit_pickup_friendly").GetClip(), 0.5f);
            else
                AudioSource.PlayOneShot(GameMaster.GetAudio("unit_pickup_enemy").GetClip(), 0.5f);
        }
        else
            gameObject.transform.localScale = originalScale;
    }

    protected override void OnOrderChanged(object target, DataChangedEventArgs<UnitOrder> e)
    {
        base.OnOrderChanged(target, e);

        if (e.NewValue != UnitOrder.None)
            AudioSource.PlayOneShot(GameMaster.GetAudio("unit_command_response").GetClip());
    }

    protected virtual void ProcessAttackRoutine(float deltaTime)
    {
        if (!AttackingTarget)
            return;

        AttackTimer += deltaTime;
        if (AttackTimer >= Attributes.ValueOf(AttributeType.ATTACK_SPEED))
        {
            AttackTimer = 0f;

            if (Target != null && Target.IsAlive() && !IsMoving)
            {
                // Attack is triggered by animation
                if (Animators.Length > 0)
                    SetAnimatorsTrigger(ActorAnimationTrigger.ATTACK);
                // No animation to trigger attack, do it manually
                else
                    TriggerAttackFromAnimation();
            }
            else
            {                
                AttackingTarget = false;
            }
        }
    }

    public virtual void TriggerCastFromAnimation()
    {
        ResetAnimatorsTrigger(ActorAnimationTrigger.CAST);
    }

    public virtual void TriggerAttackFromAnimation()
    {
        Attack();
        ResetAnimatorsTrigger(ActorAnimationTrigger.ATTACK);
    }

    protected void Attack()
    {
        if (!Target)
            return;

        if (projectilePrefab)
        {
            Projectile projectile = Projectile.Spawn(projectilePrefab, projectileOrigin.position, projectilePrefab.transform.rotation, this, Target.transform);
            projectile.damage += Attributes.ValueOf(AttributeType.DAMAGE);
        }
        else if (AttackingTarget)
        {
            // TODO: this is where we want to handle weapons, damage types, etc.
            Target.Damage(Attributes.ValueOf(AttributeType.DAMAGE), AttributeChangeCause.ATTACKED, this, DamageType.NONE);
        }

        // TODO: Play attack start sound. For siege weapons, a launch sound, archers a bow sound,
        // infantry/cavalry a metal attack sound etc. (If we want all those sounds.)
        TryPlayAttackSound();
    }

    protected virtual void ProcessHealRoutine(float deltaTime)
    {
        if (!HealingTarget)
            return;

        HealTimer += deltaTime;
        if (HealTimer >= 1f)
        {
            HealTimer = 0f;

            if (Target != null && !Target.Attributes.Get(AttributeType.HEALTH).IsMax() && !IsMoving)
            {
                // Heal triggered by animation
                if (Animators.Length > 0)
                {
                    SetAnimatorsTrigger(ActorAnimationTrigger.HEAL);   
                }
                // No animation to trigger heal, do it manually
                else
                    TriggerHealFromAnimation();
            }
            else
            {
                HealingTarget = false;
            }
        }
    }

    public virtual void TriggerHealFromAnimation()
    {
        Heal();
        ResetAnimatorsTrigger(ActorAnimationTrigger.HEAL);
    }

    // TODO: Should these be handled by the object getting healed?
    protected GameObject currentHealFx;
    protected AudioSource audioSource;
    private void Heal()
    {
        if (!Target)
            return;

        if (HealingTarget)
        {
            Target.Heal(Attributes.ValueOf(AttributeType.HEAL_RATE), AttributeChangeCause.HEALED, this);
            TryPlayHealSound();                
        }
    }

    private void TryPlayAttackSound()
    {
        if (attackClip && audioSource && !audioSource.isPlaying)
            audioSource.PlayOneShot(attackClip);

    }

    private void TryPlayHealSound()
    {
        if (healClip && audioSource && !audioSource.isPlaying)
            audioSource.PlayOneShot(healClip);
            
    }
}
