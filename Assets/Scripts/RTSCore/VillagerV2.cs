using Swordfish.Library.BehaviorTrees;
using Swordfish.Library.Types;
using Swordfish.Navigation;
using UnityEngine;

public class VillagerV2 : UnitV2
{
    public override bool IsCivilian => true;

    public ResourceGatheringType CargoType
    {
        get => CargoTypeBinding.Get();
        set => CargoTypeBinding.Set(value);
    }

    public DataBinding<ResourceGatheringType> CargoTypeBinding { get; private set; } = new();

    [Header("Tool Objects")]
    [SerializeField]
    private Transform FarmingToolObject = null;

    [SerializeField]
    private Transform MiningToolObject;

    [SerializeField]
    private Transform LumberjackToolObject;

    [SerializeField]
    private Transform BuilderToolObject;

    [SerializeField]
    private Transform FishingToolObject;

    [SerializeField]
    private Transform HuntingToolObject;

    [SerializeField]
    private Transform HuntingBackObject;

    private Transform CurrentToolObject;


    [Header("Cargo Objects")]
    [SerializeField]
    private Transform FoodCargoObject;

    [SerializeField]
    private Transform WoodCargoObject;

    [SerializeField]
    private Transform StoneCargoObject;

    [SerializeField]
    private Transform GoldCargoObject;

    private Transform CurrentCargoObject;

    public bool IsCargoFull() => Attributes.Get(AttributeConstants.CARGO).IsMax();

    protected override BehaviorTree<ActorV2> BehaviorTreeFactory()
    {
        return VillagerBehaviorTree.Get();
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeConstants.CARGO, 0f, 10f);
    }

    protected override void AttachListeners()
    {
        base.AttachListeners();
        Attributes.Get(AttributeConstants.CARGO).ValueBinding.Changed += OnCargoChanged;
    }

    protected override void CleanupListeners()
    {
        base.CleanupListeners();
        Attributes.Get(AttributeConstants.CARGO).ValueBinding.Changed -= OnCargoChanged;
    }

    public override void IssueTargetedOrder(Body body)
    {
        switch (body)
        {
            case Resource resource:
                Target = resource;
                Order = UnitOrder.Collect;
                break;

            case Fauna fauna:
                Target = fauna;
                Order = UnitOrder.Hunt;
                break;

            case Constructible constructible:
                Target = constructible;
                Order = UnitOrder.Repair;
                break;

            case Structure structure:
                Target = structure;
                if (structure.Attributes.Get(AttributeConstants.HEALTH).IsMax())
                    Order = UnitOrder.DropOff;
                else
                    Order = UnitOrder.Repair;
                break;

            case UnitV2 unit:
                Target = unit;
                if (unit.Faction != null && unit.Faction.IsAllied(Faction))
                    Order = UnitOrder.None;
                else
                    Order = UnitOrder.Attack;
                break;

            default:
                Target = body;
                Order = UnitOrder.None;
                break;
        }
    }

    public override void SetUnitType(RTSUnitType unitType)
    {
        base.SetUnitType(unitType);

        switch (unitType)
        {
            case RTSUnitType.Hunter:
                Order = UnitOrder.Hunt;
                break;
            case RTSUnitType.Lumberjack:
                OrderToCollect(ResourceGatheringType.Wood);
                break;
            case RTSUnitType.GoldMiner:
                OrderToCollect(ResourceGatheringType.Gold);
                break;
            case RTSUnitType.StoneMiner:
                OrderToCollect(ResourceGatheringType.Stone);
                break;
            case RTSUnitType.Farmer:
                OrderToCollect(ResourceGatheringType.Grain);
                break;
            case RTSUnitType.Forager:
                OrderToCollect(ResourceGatheringType.Berries);
                break;
            case RTSUnitType.Fisherman:
                OrderToCollect(ResourceGatheringType.Fish);
                break;
        }
    }

    public virtual void OrderToCollect(ResourceGatheringType resourceType, Body target = null)
    {
        Target = target;
        Order = UnitOrder.Collect;
        CargoType = resourceType;
    }

    protected virtual void OnCargoChanged(object sender, DataChangedEventArgs<float> e)
    {
        bool isCargoFull = e.NewValue == Attributes.MaxValueOf(AttributeConstants.CARGO);
        if (isCargoFull || e.NewValue == 0f)
            UpdateCurrentCargoObject(isCargoFull);
    }

    protected override void OnStateUpdate()
    {
        base.OnStateUpdate();
        UpdateCurrentToolObject();
    }

    private void UpdateCurrentToolObject()
    {
        CurrentToolObject?.gameObject.SetActive(false);
        switch (State)
        {
            case ActorAnimationState.FARMING:
                CurrentToolObject = FarmingToolObject;
                break;
            case ActorAnimationState.MINING:
                CurrentToolObject = MiningToolObject;
                break;
            case ActorAnimationState.LUMBERJACKING:
                CurrentToolObject = LumberjackToolObject;
                break;
            case ActorAnimationState.BUILDANDREPAIR:
                CurrentToolObject = BuilderToolObject;
                break;
            case ActorAnimationState.FISHING:
                CurrentToolObject = FishingToolObject;
                break;
            case ActorAnimationState.HUNTING:
                CurrentToolObject = HuntingToolObject;
                break;
            default:
                CurrentToolObject = null;
                return;
        }
        CurrentToolObject?.gameObject.SetActive(true);

        HuntingBackObject.gameObject.SetActive(HuntingToolObject.gameObject.activeSelf);
    }

    private void UpdateCurrentCargoObject(bool visible)
    {
        CurrentCargoObject?.gameObject.SetActive(false);
        switch (CargoType)
        {
            case ResourceGatheringType.Grain:
            case ResourceGatheringType.Berries:
            case ResourceGatheringType.Fish:
            case ResourceGatheringType.Meat:
                CurrentCargoObject = FoodCargoObject;
                break;
            case ResourceGatheringType.Wood:
                CurrentCargoObject = WoodCargoObject;
                break;
            case ResourceGatheringType.Stone:
                CurrentCargoObject = StoneCargoObject;
                break;
            case ResourceGatheringType.Gold:
                CurrentCargoObject = GoldCargoObject;
                break;

            default:
                CurrentCargoObject = null;
                return;
        }
        CurrentCargoObject?.gameObject.SetActive(visible);
    }
}
