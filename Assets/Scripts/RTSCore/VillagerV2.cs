using Swordfish.Library.BehaviorTrees;
using Swordfish.Library.Types;
using Swordfish.Navigation;
using UnityEngine;

public class VillagerV2 : ActorV2
{
    //  ! This is a test class don't try to actually use it.
    public override BehaviorTree<ActorV2> BehaviorTree { get; protected set; }

    public bool IsCargoFull => Attributes.Get(AttributeConstants.CARGO).IsMax();

    public override float Speed { get; protected set; } = 0.3f;
    public override int Reach { get; protected set; } = 1;

    public ResourceGatheringType CargoType
    {
        get => CargoTypeBinding.Get();
        set => CargoTypeBinding.Set(value);
    }

    public DataBinding<ResourceGatheringType> CargoTypeBinding { get; private set; } = new();

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

    public override void Initialize()
    {
        base.Initialize();

        BehaviorTree = new BehaviorTree<ActorV2>(
            new BehaviorSelector(

                new OrderIs(UnitOrder.GoTo,
                    new BehaviorSelector(
                        //  Attempt to go to
                        new BehaviorSequence(
                            new HasDestination(),
                            new GoToDestination(),
                            new ResetDestination(),
                            new ResetOrder()
                        ),
                        //  Else order is complete
                        new BehaviorSequence(
                            new ResetDestination(),
                            new ResetOrder()
                        )
                    )
                ),

                new OrderIs(UnitOrder.Collect,
                    new BehaviorSelector(
                        //  Attempt to drop off if cargo is full
                        new BehaviorSequence(
                            new IsCargoFull(),
                            new BehaviorSelector(
                                //  Try the current target
                                new CanDropOffAtTarget(),
                                //  Or get the nearest dropoff
                                new TargetNearestDropOff()
                            ),
                            new GoToTarget(),
                            new LookAtTarget(),
                            new CanDropOffAtTarget(),
                            new DropOffCargo(),
                            new TargetPrevious()
                        ),
                        //  Else attempt to collect resources
                        new BehaviorSequence(
                            new BehaviorSelector(
                                //  Try the current target
                                new CanCollectTarget(),
                                //  Or find the nearest matching resource
                                new TargetNearestCargoResource()
                            ),
                            //  Navigate to the resource
                            new GoToTarget(),
                            //  Collect the resource
                            new BehaviorSequence(
                                //  If cargo isn't full
                                new BehaviorInverter(
                                    new IsCargoFull()
                                ),
                                new CanCollectTarget(),
                                new SetCargoTypeFromTarget(),
                                new SetStateToGathering(),
                                new BehaviorDelay(1.5f,
                                    new CollectCargo()
                                )
                            )
                        ),
                        //  Else order is complete
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                ),

                new OrderIs(UnitOrder.DropOff,
                    new BehaviorSelector(
                        //  Attempt to drop off at the target
                        new BehaviorSequence(
                            new HasTarget(),
                            new GoToTarget(),
                            new CanDropOffAtTarget(),
                            new DropOffCargo(),
                            new ResetTarget(),
                            new ResetOrder()
                        ),
                        //  Else order is complete
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                ),

                new OrderIs(UnitOrder.Hunt,
                    new BehaviorSelector(
                        //  Attempt to drop off if cargo is full
                        new BehaviorSequence(
                            new IsCargoFull(),
                            new BehaviorSelector(
                                //  Try the current target
                                new CanDropOffAtTarget(),
                                //  Or get the nearest dropoff
                                new TargetNearestDropOff()
                            ),
                            new GoToTarget(),
                            new LookAtTarget(),
                            new CanDropOffAtTarget(),
                            new DropOffCargo(),
                            new TargetPrevious()
                        ),
                        //  Else attempt to hunt a target
                        new BehaviorSequence(
                            new BehaviorSelector(
                                //  Try the current target
                                new HasTarget(),
                                //  Or find the nearest matching resource
                                new TargetNearestFauna()
                            ),
                            //  Navigate to the target
                            new GoToTarget(),
                            new BehaviorSelector(
                                //  Try to collect the target
                                new BehaviorSequence(
                                    //  If cargo isn't full
                                    new BehaviorInverter(
                                        new IsCargoFull()
                                    ),
                                    new CanCollectTarget(),
                                    new SetCargoTypeFromTarget(),
                                    new SetStateToGathering(),
                                    new BehaviorDelay(1.5f,
                                        new CollectCargo()
                                    )
                                ),
                                //  Else try to attack the target
                                new BehaviorSelector(
                                    new BehaviorSequence(
                                        new SetActorState(ActorAnimationState.HUNTING),
                                        new BehaviorDelay(1.5f,
                                            new AttackTarget()
                                        ),
                                        new SetCargoType(ResourceGatheringType.Meat),
                                        new TargetNearestCargoResource()
                                    )
                                )
                            )
                        ),
                        //  Else order is complete
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                ),

                new OrderIs(UnitOrder.Attack,
                    new BehaviorSelector(
                        //  Attempt to chase and attack the target
                        new BehaviorSequence(
                            new HasTarget(),
                            new GoToTarget(),
                            new SetActorState(ActorAnimationState.HUNTING),
                            new BehaviorDelay(1.5f,
                                new AttackTarget()
                            ),
                            new ResetTarget(),
                            new ResetOrder()
                        ),
                        //  Else order is complete
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                ),

                new OrderIs(UnitOrder.Repair,
                    new BehaviorSelector(
                        //  Attempt to repair the target
                        new BehaviorSequence(
                            new HasTarget(),
                            new GoToTarget(),
                            new SetActorState(ActorAnimationState.BUILDANDREPAIR),
                            new BehaviorDelay(1.5f,
                                new HealTarget()
                            ),
                            new ResetTarget(),
                            new ResetOrder()
                        ),
                        //  Else order is complete
                        new BehaviorSequence(
                            new ResetTarget(),
                            new ResetOrder()
                        )
                    )
                ),

                //  Try to navigate to our current destination
                new IfHasDestination(
                    new BehaviorSequence(
                        new GoToDestination(),
                        new ResetDestination()
                    )
                ),

                //  Try to navigate to our current target
                new IfHasTarget(
                    new BehaviorSequence(
                        new GoToTarget(),
                        new ResetTarget()
                    )
                ),

                new SetActorState(ActorAnimationState.IDLE)
            )
        );
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.TryAdd(AttributeConstants.CARGO, 0f, 10f);
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

    protected override void OnOrderChanged(object target, DataChangedEventArgs<UnitOrder> e)
    {
        base.OnOrderChanged(target, e);
        AudioSource.PlayOneShot(GameMaster.GetAudio("unit_command_response").GetClip());
    }

    protected override void OnDeath(DeathEvent e)
    {
        base.OnDeath(e);

        Frozen = true;
        CurrentPath = null;
        Order = UnitOrder.None;
        Destination = null;
        Target = null;

        State = Random.Range(1, 100) < 50 ? ActorAnimationState.DYING : ActorAnimationState.DYING2;
        AudioSource.PlayOneShot(GameMaster.GetAudio("unit_death").GetClip());
        Destroy(gameObject, GameMaster.Instance.unitCorpseDecayTime);
    }

    protected virtual void OnCargoChanged(object sender, DataChangedEventArgs<float> e)
    {
        bool isCargoFull = e.NewValue == Attributes.MaxValueOf(AttributeConstants.CARGO);

        if (isCargoFull || e.NewValue == 0f)
            UpdateCurrentCargoObject(isCargoFull);
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
