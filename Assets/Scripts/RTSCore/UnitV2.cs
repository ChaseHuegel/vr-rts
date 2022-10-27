using Swordfish;
using Swordfish.Library.Types;
using Swordfish.Navigation;
using UnityEngine;

public abstract class UnitV2 : ActorV2
{
    public abstract bool IsCivilian { get; }

    public UnitData UnitData => m_UnitData ??= GameMaster.GetUnit(UnitType);
    private UnitData m_UnitData;

    public bool Attacking
    {
        get => AttackingBinding.Get();
        set => AttackingBinding.Set(value);
    }

    public DataBinding<bool> AttackingBinding { get; private set; } = new();

    [Header("Unit Settings")]
    [SerializeField]
    private RTSUnitType UnitType;

    private float AttackTimer;

    protected override void Start()
    {
        base.Start();
        SetUnitType(UnitType);
    }

    protected override void Update()
    {
        base.Update();
        if (!Frozen)
            ProcessAttackRoutine(Time.deltaTime);
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeConstants.DAMAGE, 1f);
        Attributes.AddOrUpdate(AttributeConstants.ATTACK_SPEED, 1.25f);
    }

    public virtual void SetUnitType(RTSUnitType unitType)
    {
        UnitType = unitType;
        m_UnitData = GameMaster.GetUnit(UnitType);
        OnLoadUnitData(m_UnitData);
    }

    protected virtual void OnLoadUnitData(UnitData data)
    {
        Attributes.Get(AttributeConstants.REACH).Value = data.attackRange;
        Attributes.Get(AttributeConstants.DAMAGE).MaxValue = data.attackDamage;
        Attributes.Get(AttributeConstants.HEALTH).MaxValue = data.maxHitPoints;
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
            if (Faction != null && Faction.IsSameFaction(PlayerManager.instance.faction))
                AudioSource.PlayOneShot(GameMaster.GetAudio("unit_pickup_friendly").GetClip(), 0.5f);
            else
                AudioSource.PlayOneShot(GameMaster.GetAudio("unit_pickup_enemy").GetClip(), 0.5f);
        }
    }

    protected override void OnOrderChanged(object target, DataChangedEventArgs<UnitOrder> e)
    {
        base.OnOrderChanged(target, e);

        if (e.NewValue != UnitOrder.None)
            AudioSource.PlayOneShot(GameMaster.GetAudio("unit_command_response").GetClip());
    }

    protected virtual void ProcessAttackRoutine(float deltaTime)
    {
        if (!Attacking)
        {
            if (State == ActorAnimationState.ATTACKING)
                State = ActorAnimationState.IDLE;

            return;
        }

        State = ActorAnimationState.ATTACKING;

        AttackTimer += deltaTime;
        if (AttackTimer >= Attributes.ValueOf(AttributeConstants.ATTACK_SPEED))
        {
            AttackTimer = 0f;

            if (Target != null && Target.IsAlive() && GetDistanceTo(Target.GetPosition().x, Target.GetPosition().y) <= Attributes.ValueOf(AttributeConstants.REACH))
            {
                Attack(Target);
            }
            else
            {
                Attacking = false;
            }
        }
    }

    private void Attack(Damageable victim)
    {
        //  TODO this is where we want to handle weapons, damage types, etc.
        victim.Damage(Attributes.ValueOf(AttributeConstants.DAMAGE), AttributeChangeCause.ATTACKED, this, DamageType.NONE);
    }

}
