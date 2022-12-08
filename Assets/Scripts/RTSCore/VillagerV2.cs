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
    private AttributeType currentCollectRate = AttributeType.COLLECT_RATE;

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

    public bool IsCargoFull() => Attributes.Get(AttributeType.CARGO).IsMax();

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
        Attributes.AddOrUpdate(AttributeType.CARGO, 0f, 10f);
        Attributes.AddOrUpdate(AttributeType.WOOD_CARGO, 0f, 10f);
        Attributes.AddOrUpdate(AttributeType.STONE_CARGO, 0f, 10f);
        Attributes.AddOrUpdate(AttributeType.GOLD_CARGO, 0f, 10f);
        Attributes.AddOrUpdate(AttributeType.FOOD_CARGO, 0f, 10f);
        Attributes.AddOrUpdate(AttributeType.COLLECT_RATE, 1f);
        Attributes.AddOrUpdate(AttributeType.BUILD_RATE, 1f);
        Attributes.AddOrUpdate(AttributeType.EFFICIENCY, 1f);
    }

    protected override void OnLoadUnitData(UnitData data)
    {
        base.OnLoadUnitData(data);
        Attributes.AddOrUpdate(AttributeType.COLLECT_RATE, data.collectRate, data.collectRate);
        Attributes.AddOrUpdate(AttributeType.HEAL_RATE, data.healRate, data.healRate);
        Attributes.AddOrUpdate(AttributeType.BUILD_RATE, data.buildRate, data.buildRate);
        Attributes.AddOrUpdate(AttributeType.GOLD_MINING_RATE, data.goldMiningRate, data.goldMiningRate);
        Attributes.AddOrUpdate(AttributeType.STONE_MINING_RATE, data.stoneMiningRate, data.stoneMiningRate);
        Attributes.AddOrUpdate(AttributeType.LUMBERJACKING_RATE, data.lumberjackingRate, data.lumberjackingRate);
        Attributes.AddOrUpdate(AttributeType.FARMING_RATE, data.farmingRate, data.farmingRate);
        Attributes.AddOrUpdate(AttributeType.FISHING_RATE, data.fishingRate, data.fishingRate);
        Attributes.AddOrUpdate(AttributeType.FORAGING_RATE, data.foragingRate, data.foragingRate);
        Attributes.AddOrUpdate(AttributeType.HUNTING_RATE, data.huntingRate, data.huntingRate);
        Attributes.AddOrUpdate(AttributeType.CARGO, 0, data.maxCargo);
        Attributes.AddOrUpdate(AttributeType.WOOD_CARGO, 0, data.maxWoodCargo);
        Attributes.AddOrUpdate(AttributeType.STONE_CARGO, 0, data.maxStoneCargo);
        Attributes.AddOrUpdate(AttributeType.GOLD_CARGO, 0, data.maxGoldCargo);
        Attributes.AddOrUpdate(AttributeType.FOOD_CARGO, 0, data.maxFoodCargo);
    }

    protected override void AttachListeners()
    {
        base.AttachListeners();
        Attributes.Get(AttributeType.CARGO).ValueBinding.Changed += OnCargoChanged;
        // TODO: add a listener on CargoType change to update COLLECT_RATE appropriately
    }

    protected override void CleanupListeners()
    {
        base.CleanupListeners();
        Attributes.Get(AttributeType.CARGO).ValueBinding.Changed -= OnCargoChanged;
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
                currentCollectRate = AttributeType.HUNTING_RATE;
                break;

            case Constructible constructible:
                Target = constructible;
                if (constructible.buildingData is WallData)
                    Order = UnitOrder.BuildWalls;
                else if (constructible.buildingData.buildingType == BuildingType.FactionedResource)
                    Order = UnitOrder.BuildAndFarm;
                else                    
                    Order = UnitOrder.Heal;

                currentCollectRate = AttributeType.BUILD_RATE;
                break;

            case Structure structure:
                Target = structure;
                if (structure.buildingData is WallData)
                {
                    Order = UnitOrder.BuildWalls;
                    currentCollectRate = AttributeType.HEAL_RATE;
                }
                else if (structure.Attributes.Get(AttributeType.HEALTH).IsMax())
                    Order = UnitOrder.DropOff;
                else
                {
                    currentCollectRate = AttributeType.HEAL_RATE;
                    Order = UnitOrder.Heal;
                }
                break;

            case UnitV2 unit:
                Target = unit;
                if (Faction.IsAllied(Faction))
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
        SetCurrentCollectRateAndCargoMaxValue(resourceType);
    }

    protected virtual void OnCargoChanged(object sender, DataChangedEventArgs<float> e)
    {
        bool isCargoFull = e.NewValue == Attributes.MaxValueOf(AttributeType.CARGO);
        if (isCargoFull || e.NewValue == 0.0f)
            UpdateCurrentCargoObject(isCargoFull);
    }

    protected virtual void SetCurrentCollectRateAndCargoMaxValue(ResourceGatheringType resourceGatheringType)
    {
        float newMaxValue = Attributes.Get(AttributeType.CARGO).MaxValue;

        switch (resourceGatheringType)
        {
            case ResourceGatheringType.Grain:
                currentCollectRate = AttributeType.FARMING_RATE;
                newMaxValue = Attributes.Get(AttributeType.FOOD_CARGO).MaxValue;;
                break;
            case ResourceGatheringType.Berries:
                currentCollectRate = AttributeType.FORAGING_RATE;
                newMaxValue = Attributes.Get(AttributeType.FOOD_CARGO).MaxValue;
                break;
            case ResourceGatheringType.Fish:
                currentCollectRate = AttributeType.FISHING_RATE;
                newMaxValue = Attributes.Get(AttributeType.FOOD_CARGO).MaxValue;
                break;
            case ResourceGatheringType.Meat:
                currentCollectRate = AttributeType.HUNTING_RATE;
                newMaxValue = Attributes.Get(AttributeType.FOOD_CARGO).MaxValue;
                break;
            case ResourceGatheringType.Wood:
                currentCollectRate = AttributeType.LUMBERJACKING_RATE;
                newMaxValue = Attributes.Get(AttributeType.WOOD_CARGO).MaxValue;
                break;
            case ResourceGatheringType.Stone:
                currentCollectRate = AttributeType.STONE_MINING_RATE;
                newMaxValue = Attributes.Get(AttributeType.STONE_CARGO).MaxValue;
                break;
            case ResourceGatheringType.Gold:
                currentCollectRate = AttributeType.GOLD_MINING_RATE;
                newMaxValue = Attributes.Get(AttributeType.GOLD_CARGO).MaxValue;
                break;
        }

        Attributes.Get(AttributeType.CARGO).MaxValue = newMaxValue;
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
            case ActorAnimationState.HEAL:
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
        float collectRateModdedByEfficiency = Attributes.ValueOf(currentCollectRate) * Attributes.ValueOf(AttributeType.EFFICIENCY);
        float amount = resource.TryRemove(Attributes.ValueOf(currentCollectRate));
        Attributes.Get(AttributeType.CARGO).Value += amount + collectRateModdedByEfficiency;

    }
}
