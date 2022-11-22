using Swordfish;
using Swordfish.Library.Types;
using Swordfish.Navigation;
using UnityEngine;

public abstract class UnitV2 : ActorV2
{
    public abstract bool IsCivilian { get; }

    public UnitData unitData;

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
        Attributes.AddOrUpdate(AttributeConstants.DAMAGE, 1f);
        Attributes.AddOrUpdate(AttributeConstants.ATTACK_SPEED, 1f);
        Attributes.AddOrUpdate(AttributeConstants.ATTACK_RANGE, Attributes.ValueOf(AttributeConstants.REACH));
        Attributes.AddOrUpdate(AttributeConstants.HEAL_RATE, 1f);
    }

    protected virtual void OnLoadUnitData(UnitData data)
    {
        Attributes.Get(AttributeConstants.ATTACK_RANGE).Value = data.attackRange;
        Attributes.Get(AttributeConstants.DAMAGE).MaxValue = data.attackDamage;
        Attributes.Get(AttributeConstants.HEALTH).MaxValue = data.maximumHitPoints;
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
        Animator?.SetInteger("ActorAnimationState", (int)deathState);
        AudioSource.PlayOneShot(GameMaster.GetAudio("unit_death").GetClip());
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
        if (AttackTimer >= Attributes.ValueOf(AttributeConstants.ATTACK_SPEED))
        {
            AttackTimer = 0f;

            if (Target != null && Target.IsAlive() && !IsMoving)
            {
                Attack(Target);
            }
            else
            {
                AttackingTarget = false;
            }
        }
    }

    private void Attack(Damageable victim)
    {
        // TODO: this is where we want to handle weapons, damage types, etc.
        victim.Damage(Attributes.ValueOf(AttributeConstants.DAMAGE), AttributeChangeCause.ATTACKED, this, DamageType.NONE);
    }

    protected virtual void ProcessHealRoutine(float deltaTime)
    {
        if (!HealingTarget)
            return;

        HealTimer += deltaTime;
        if (HealTimer >= 1f)
        {
            HealTimer = 0f;

            if (Target != null && !Target.Attributes.Get(AttributeConstants.HEALTH).IsMax() && !IsMoving)
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
        target.Heal(Attributes.ValueOf(AttributeConstants.HEAL_RATE), AttributeChangeCause.HEALED, this);
    }

}
