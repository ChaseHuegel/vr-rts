using Swordfish.Library.Types;
using Swordfish.Navigation;
using UnityEngine;

public abstract class UnitV2 : ActorV2
{
    public abstract bool IsCivilian { get; }

    public UnitData UnitData => m_UnitData ??= GameMaster.GetUnit(UnitType);
    private UnitData m_UnitData;

    [Header("Unit Settings")]
    [SerializeField]
    private RTSUnitType UnitType;

    protected override void Start()
    {
        base.Start();
        SetUnitType(UnitType);
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeConstants.DAMAGE, 1f);
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

}
