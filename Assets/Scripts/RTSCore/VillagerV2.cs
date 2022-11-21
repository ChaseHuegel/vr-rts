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

    public bool CollectingTarget
    {
        get => CollectingTargetBinding.Get();
        set => CollectingTargetBinding.Set(value);
    }

    public DataBinding<ResourceGatheringType> CargoTypeBinding { get; private set; } = new();
    public DataBinding<bool> CollectingTargetBinding { get; private set; } = new();

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
    private float CollectTimer;

    public bool IsCargoFull() => Attributes.Get(AttributeConstants.CARGO).IsMax();

    protected override void Update()
    {
        base.Update();
        if (!Frozen)
            ProcessCollectRoutine(Time.deltaTime);
    }

    protected override BehaviorTree<ActorV2> BehaviorTreeFactory()
    {
        return VillagerBehaviorTree.Value;
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeConstants.CARGO, 0f, 10f);
        Attributes.AddOrUpdate(AttributeConstants.COLLECT_RATE, 1f);
    }

    protected override void OnLoadUnitData(UnitData data)
    {
        base.OnLoadUnitData(data);
        Attributes.Get(AttributeConstants.COLLECT_RATE).Value = data.lumberjackingRate;
        Attributes.Get(AttributeConstants.HEAL_RATE).MaxValue = data.buildRate;
    }

    protected override void AttachListeners()
    {
        base.AttachListeners();
        Attributes.Get(AttributeConstants.CARGO).ValueBinding.Changed += OnCargoChanged;
        // TODO: add a listener on CargoType change to update COLLECT_RATE appropriately
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
                if (constructible.buildingData is WallData)
                    Order = UnitOrder.BuildWalls;
                else
                    Order = UnitOrder.Repair;
                break;

            case Structure structure:
                Target = structure;
                if (structure.buildingData is WallData)
                    Order = UnitOrder.BuildWalls;
                else if (structure.Attributes.Get(AttributeConstants.HEALTH).IsMax())
                    Order = UnitOrder.DropOff;
                else
                    Order = UnitOrder.Repair;
                break;

            case UnitV2 unit:
                Target = unit;
                if (unit.Faction.IsAllied(Faction))
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

    public virtual void OrderToCollect(ResourceGatheringType resourceType, Body target = null)
    {
        Target = target;
        Order = UnitOrder.Collect;
        CargoType = resourceType;
    }

    protected virtual void OnCargoChanged(object sender, DataChangedEventArgs<float> e)
    {
        bool isCargoFull = e.NewValue == Attributes.MaxValueOf(AttributeConstants.CARGO);
        if (isCargoFull || e.NewValue == 0.0f)
            UpdateCurrentCargoObject(isCargoFull);
    }

    protected override void OnStateUpdate()
    {
        base.OnStateUpdate();
        UpdateCurrentToolObject();
    }

    protected virtual void UpdateCurrentToolObject()
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

        HuntingBackObject?.gameObject.SetActive(HuntingToolObject?.gameObject.activeSelf ?? false);
    }

    protected virtual void UpdateCurrentCargoObject(bool visible)
    {
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

    protected virtual void ProcessCollectRoutine(float deltaTime)
    {
        if (!CollectingTarget)
            return;

        CollectTimer += deltaTime;
        if (CollectTimer >= 1f)
        {
            CollectTimer = 0f;

            if (Target is Resource resource && !IsMoving)
            {
                CollectResource(resource);
            }
            else
            {
                CollectingTarget = false;
            }
        }
    }

    private void CollectResource(Resource resource)
    {
        Attributes.Get(AttributeConstants.CARGO).Value += resource.TryRemove(Attributes.ValueOf(AttributeConstants.COLLECT_RATE));
    }
}
