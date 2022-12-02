using Swordfish;
using Swordfish.Library.Types;
using Swordfish.Navigation;
using UnityEngine;

public abstract class UnitV2 : ActorV2
{
    public abstract bool IsCivilian { get; }
    public GameObject healFxPrefab;
    public GameObject damagedFxPrefab;
    public GameObject deathFxPrefab;
    public GameObject projectilePrefab;
    public Transform projectileOrigin;
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

    private float AttackTimer;
    private float HealTimer;

    private Vector3 originalScale = Vector3.one;

    protected override void Start()
    {        
        base.Start();
        OnLoadUnitData(unitData);

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

        if (PlayerManager.AllAttributeBonuses.TryGetValue(unitData, out var bonuses))
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

    protected virtual void OnAttributeBonusChanged(object sender, PlayerManager.AttributeBonusChangeEvent e)
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
        PlayerManager.OnAttributeBonusChangedEvent += OnAttributeBonusChanged;
    }

    protected override void CleanupListeners()
    {
        base.CleanupListeners();
        PlayerManager.OnAttributeBonusChangedEvent -= OnAttributeBonusChanged;
    }

    protected override void OnFallingOntoBody(Body body)
    {
        base.OnFallingOntoBody(body);
        IssueTargetedOrder(body);
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
                SetAnimatorsTrigger(ActorAnimationTrigger.ATTACK);
            }
            else
            {                
                AttackingTarget = false;
            }
        }
    }

    public void ExecuteAttackFromAnimation()
    {
        if (!Target)
            return;

        if (projectilePrefab)
        {
            Projectile projectile = Projectile.Spawn(projectilePrefab, projectileOrigin.position, projectilePrefab.transform.rotation, this, Target.transform);
            projectile.damage += Attributes.ValueOf(AttributeType.DAMAGE);
            //SetAnimatorsInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);
        }
        else if (AttackingTarget)
            // TODO: this is where we want to handle weapons, damage types, etc.
            Target.Damage(Attributes.ValueOf(AttributeType.DAMAGE), AttributeChangeCause.ATTACKED, this, DamageType.NONE);

        ResetAnimatorsTrigger(ActorAnimationTrigger.ATTACK);
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
                Heal(Target);
            }
            else
            {
                HealingTarget = false;
            }
        }
    }

    private void Heal(Damageable target)
    {
        target.Heal(Attributes.ValueOf(AttributeType.HEAL_RATE), AttributeChangeCause.HEALED, this);              
    }

}
