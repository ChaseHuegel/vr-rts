using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Damageable))]
public class Villager : Unit
{
    [Header("Villager")]
    protected ResourceGatheringType currentResource;
    protected ResourceGatheringType previousResource;
    protected GoalTransportResource transportGoal;

    [SerializeField]
    protected float currentCargo = 0;

    [Header ("Visuals")]
    public GameObject grainCargoDisplayObject;
    public GameObject woodCargoDisplayObject;
    public GameObject stoneCargoDisplayObject;
    public GameObject goldCargoDisplayObject;
    public GameObject grainHandToolDisplayObject;
    public GameObject woodHandToolDisplayObject;
    public GameObject stoneHandToolDisplayObject;
    public GameObject goldHandToolDisplayObject;
    public GameObject builderHandToolDisplayObject;
    public GameObject fishermanHandToolDisplayObject;
    public GameObject hunterHandToolDisplayObject;

    protected GameObject currentCargoDisplayObject;
    protected GameObject currentHandToolDisplayObject;
    public VillagerHoverMenu villagerHoverMenu;
    protected float detachFromHandTime;
    protected PathfindingGoal currentGoalFound;
    protected PathfindingGoal previousGoalFound;

    public bool IsCargoFull() { return currentCargo >= rtsUnitTypeData.maxCargo; }
    public bool HasCargo() { return currentCargo > 0; }

    public override void Initialize()
    {
        base.Initialize();
        HookIntoEvents();

        SetUnitType(rtsUnitType);

        if(IsSameFaction(playerManager.factionId))
            playerManager.AddToPopulation((Unit)this);
    }

    public void HookIntoEvents()
    {
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;
        PathfindingGoal.OnGoalChangeEvent += OnGoalChange;
        Damageable.OnDeathEvent += OnDeath;
    }

#region Hand Events

    public override void OnHandHoverBegin(Hand hand)
    {
        base.OnHandHoverBegin(hand);
        villagerHoverMenu.Show();
    }

    public override void OnHandHoverEnd(Hand hand)
    {
        base.OnHandHoverEnd(hand);
        villagerHoverMenu.Hide();
    }

    public override void OnAttachedToHand(Hand hand)
    {
        base.OnAttachedToHand(hand);
        villagerHoverMenu.Show();
    }

    public override void OnDetachedFromHand(Hand hand)
    {
        base.OnDetachedFromHand(hand);
        villagerHoverMenu.Hide();
        detachFromHandTime = Time.time;
    }

#endregion

#region Event Handlers

    public void OnDeath(object sender, Damageable.DeathEvent e)
    {
        if (e.victim != AttributeHandler)
            return;

        if (!isDying)
        {
            isDying = true;
            Freeze();
            ResetAI();

            if (UnityEngine.Random.Range(1, 100) < 50)
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.DYING);
            else
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.DYING2);

            audioSource.PlayOneShot(GameMaster.GetAudio("unit_death").GetClip(), 0.5f);
            Destroy(this.gameObject, GameMaster.Instance.unitCorpseDecayTime);
        }
    }

    // This is is used to reenable the character after they have been
    // released from the hand AND after they have landed somewhere.
    public override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);

        if (!wasThrownOrDropped)
            return;

        // Don't wait for a collision indefinitely.
        if (Time.time - detachFromHandTime >= 3.0f)
        {
            wasThrownOrDropped = false;
            return;
        }

        Unfreeze();

        Resource node = collision.gameObject.GetComponent<Resource>();
        if (node)
        {
            switch (node.type)
            {
                case ResourceGatheringType.Gold:
                    SetUnitType(RTSUnitType.GoldMiner);
                    break;

                case ResourceGatheringType.Grain:
                    SetUnitType(RTSUnitType.Farmer);
                    break;

                case ResourceGatheringType.Berries:
                    SetUnitType(RTSUnitType.Forager);
                    break;

                case ResourceGatheringType.Meat:
                    SetUnitType(RTSUnitType.Hunter);
                    break;

                case ResourceGatheringType.Wood:
                    SetUnitType(RTSUnitType.Lumberjack);
                    break;

                case ResourceGatheringType.Stone:
                    SetUnitType(RTSUnitType.StoneMiner);
                    break;

                case ResourceGatheringType.Fish:
                    SetUnitType(RTSUnitType.Fisherman);
                    break;

                default:
                    break;
            }

            ResetAI();
            return;
        }

        Fauna fauna = collision.gameObject.GetComponent<Fauna>();
        if (fauna)
        {
            SetUnitType(RTSUnitType.Hunter);
            ResetAI();
            return;
        }

        Structure building = collision.gameObject.GetComponentInParent<Structure>();
        if (building)
        {
            SetUnitType(RTSUnitType.Builder);
            ResetAI();
            return;
        }
    }

    public void OnGoalChange(object sender, PathfindingGoal.GoalChangeEvent e)
    {
        if (e.actor != this) return;

        if (HasTargetChanged())
        {
            //Resource resource = previousGoalTarget?.GetFirstOccupant<Resource>();
            // if (resource)
            //     //resource.interactors--;
            //     resource.RemoveInteractor(this);
        }

        ChangeEquippedItems(e.goal);
    }

    public void OnGoalFound(object sender, PathfindingGoal.GoalFoundEvent e)
    {
        if (e.actor != this) return;

        Villager villager = (Villager)e.actor;

        if (e.cell != previousGoalTarget)
        {
            Resource resource = previousGoalTarget?.GetFirstOccupant<Resource>();
            if (resource) resource.RemoveInteractor(this);
        }

        maxGoalInteractRange = rtsUnitTypeData.attackRange;

        //  Need C# 7 in Unity for switching by type!!!
        if (e.goal is GoalGatherResource && !villager.IsCargoFull())
        {
            villager.state = UnitState.GATHERING;
            villager.currentResource = ((GoalGatherResource)e.goal).type;
            currentGoalFound = e.goal;
            DisplayCargo(false);

            Resource resource = e.cell?.GetFirstOccupant<Resource>();
            if (resource.AddInteractor(this))
                return;
        }
        else if (e.goal is GoalTransportResource && villager.HasCargo())
        {
            villager.state = UnitState.TRANSPORTING;
            currentGoalFound = e.goal;
            DisplayCargo(true);
            return;
        }
        else if (e.goal is GoalHuntFauna && !villager.IsCargoFull())
        {
            maxGoalInteractRange = GameMaster.GetUnit(RTSUnitType.Hunter).attackRange;
            villager.state = UnitState.GATHERING;
            currentGoalFound = e.goal;
            //DisplayCargo(true);
            return;
        }
        else if (e.goal is GoalBuildRepair)
        {
            villager.state = UnitState.BUILDANDREPAIR;
            currentGoalFound = e.goal;
            return;
        }

        //  default cancel the goal so that another can take priority
        ResetGoal();
        e.Cancel();
    }

    public void OnGoalInteract(object sender, PathfindingGoal.GoalInteractEvent e)
    {
        if (e.actor != this || isHeld)
            return;

        Villager villager = (Villager)e.actor;
        Resource resource = e.cell.GetOccupant<Resource>();
        Structure structure = e.cell.GetOccupant<Structure>();
        Constructible construction = e.cell.GetOccupant<Constructible>();
        Fauna fauna = e.cell.GetOccupant<Fauna>();

        if  (e.goal is GoalHuntFauna && villager.TryHunt(fauna) ||
            e.goal is GoalGatherResource && villager.TryGather(resource) ||
            e.goal is GoalTransportResource && villager.TryDropoff(structure) ||
            e.goal is GoalBuildRepair && (villager.TryRepair(structure) ||
            villager.TryBuild(construction)))
        {
            return;
        }

        //  default cancel the interaction
        ResetGoal();
        e.Cancel();
    }

    #endregion

    public override void Tick()
    {
        if (isHeld || isDying)
            return;

        base.Tick();

        //  Transport type always matches what our current resource is
        if (transportGoal != null) transportGoal.type = currentResource;

        GotoNearestGoalWithPriority();

        switch (state)
        {
            case UnitState.ROAMING:
            case UnitState.GATHERING:
            case UnitState.TRANSPORTING:
            case UnitState.BUILDANDREPAIR:
            case UnitState.IDLE:
            break;
        }

        if (IsMoving() )
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.MOVING);
        // else
        //     animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);

        if (TaskChanged())
            ChangeEquippedItems(currentGoal);

        previousState = state;
        previousResource = currentResource;
    }

    public bool TaskChanged()
    {
        return StateChanged() && previousResource != currentResource;
    }

    bool StateChanged()
    {
        return state != previousState;
    }

     // Used by animator to play sound effects
    public void AnimatorPlayAudio(string clipName)
    {
        AudioSource.PlayClipAtPoint(GameMaster.GetAudio(clipName).GetClip(), transform.position, 0.75f);
    }

    public void Do_Fucking_Lumberjacking_Where_I_Fucking_Told_You_To(Resource resource)
    {
        SetUnitType(RTSUnitType.Lumberjack);
        ResetAI();
        transportGoal = goals.Add<GoalTransportResource>();

        GoalGatherResource getTheFuckingWood = new GoalGatherResource();
        getTheFuckingWood.type = ResourceGatheringType.Wood;
        currentGoal = getTheFuckingWood;
        currentGoalTarget = resource.GetCellAtGrid();
        //Goto(currentGoalTarget.x, currentGoalTarget.y);
        PathfindingGoal.TryGoal((Actor)this, World.at(resource.gridPosition), getTheFuckingWood);
        // villager.GotoForced(gridPosition.x, gridPosition.y);
        // villager.ResetGoal();

    }

    public void GotoPosition(Vector3 position)
    {
        foreach (PathfindingGoal goal in GetGoals())
        {
            goal.active = false;
        }

        ResetAI();
        GotoForced(World.ToWorldSpace(position));
    }

    // TODO: Should unitType be changed to unitTask or unitJob?
    public override void SetUnitType(RTSUnitType unitType)
    {
        base.SetUnitType(unitType);
        // SetUnitData(GameMaster.Instance.unitDatabase.Get(unitType));

        // Turn off all goals except the transport goal.
        goals.Clear();
        transportGoal = goals.Add<GoalTransportResource>();

        switch (unitType)
        {
            case RTSUnitType.Builder:
                state = UnitState.BUILDANDREPAIR;
                currentResource = ResourceGatheringType.None;
                goals.Add<GoalBuildRepair>();
                break;

            case RTSUnitType.Farmer:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Grain;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Grain;
                break;

            case RTSUnitType.Forager:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Berries;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Berries;
                break;

            case RTSUnitType.Hunter:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Meat;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Meat;
                goals.Add<GoalHuntFauna>();
                break;

            case RTSUnitType.Fisherman:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Fish;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Fish;
                break;

            case RTSUnitType.Lumberjack:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Wood;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Wood;
                break;

            case RTSUnitType.GoldMiner:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Gold;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Gold;
                break;

            case RTSUnitType.StoneMiner:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Stone;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Stone;
                break;

            case RTSUnitType.Drifter:
                state = UnitState.ROAMING;
                currentResource = ResourceGatheringType.None;
                goals.Add<GoalBuildRepair>();
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Grain;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Berries;
                goals.Add<GoalHuntFauna>();
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Meat;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Fish;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Gold;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Stone;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Wood;
                break;

            default:
                break;
        }


        ResetPath();
        PlayChangeTaskAudio();
    }

    public void PlayChangeTaskAudio()
    {
        // TODO: Just needs to be changed, for now use unit_command_response
        audioSource.clip = GameMaster.GetAudio("unit_command_response").GetClip();
        audioSource.Play();

        // if (state == UnitState.GATHERING)
        // {
        //     switch (currentResource)
        //     {
        //         case ResourceGatheringType.Gold:
        //         case ResourceGatheringType.Stone:
        //             audioSource.clip = GameMaster.GetAudio("miner").GetClip();
        //             audioSource.Play();
        //             break;

        //         case ResourceGatheringType.Grain:
        //         case ResourceGatheringType.Berries:
        //         case ResourceGatheringType.Meat:
        //         case ResourceGatheringType.Fish:
        //             audioSource.clip = GameMaster.GetAudio("farmer").GetClip();
        //             audioSource.Play();
        //             break;

        //         case ResourceGatheringType.Wood:
        //             audioSource.clip = GameMaster.GetAudio("lumberjack").GetClip();
        //             audioSource.Play();
        //             break;

        //         default:
        //             break;
        //     }
        // }
        // else if (state == UnitState.BUILDANDREPAIR)
        // {
        //     audioSource.clip = GameMaster.GetAudio("builder").GetClip();
        //     audioSource.Play();
        // }
    }

    /// <summary>
    /// Changes equipped items based on the goal. If goal is null, clears hands
    /// of all items.
    /// </summary>
    /// <param name="goal">The goal that should be equipped for.</param>
    public void ChangeEquippedItems(PathfindingGoal goal = null)
    {
        if (currentHandToolDisplayObject)
            currentHandToolDisplayObject.SetActive(false);

        if (goal == null)
            return;

        if (goal is GoalGatherResource)
        {
            switch (((GoalGatherResource)goal).type)
            {
                case ResourceGatheringType.Gold:
                    goldHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = goldHandToolDisplayObject;
                    return;

                case ResourceGatheringType.Stone:
                    stoneHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = stoneHandToolDisplayObject;
                    return;

                case ResourceGatheringType.Grain:
                    grainHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = grainHandToolDisplayObject;
                    return;

                case ResourceGatheringType.Berries:
                case ResourceGatheringType.None:
                case ResourceGatheringType.Meat:
                    // Equip nothing.
                    return;

                case ResourceGatheringType.Fish:
                    fishermanHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = fishermanHandToolDisplayObject;
                    return;

                case ResourceGatheringType.Wood:
                    woodHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = woodHandToolDisplayObject;
                    return;

                default:
                    return;
            }
        }
        else if (goal is GoalBuildRepair)
        {
            builderHandToolDisplayObject.SetActive(true);
            currentHandToolDisplayObject = builderHandToolDisplayObject;
            return;
        }
        else if (goal is GoalHuntFauna)
        {
            hunterHandToolDisplayObject.SetActive(true);
            currentHandToolDisplayObject = hunterHandToolDisplayObject;
            return;
        }
    }

    private void DisplayCargo(bool visible)
    {
        if (currentCargoDisplayObject)
            currentCargoDisplayObject.SetActive(false);

        switch (currentResource)
        {
            case ResourceGatheringType.Grain:
            case ResourceGatheringType.Berries:
            case ResourceGatheringType.Meat:
            case ResourceGatheringType.Fish:
                grainCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = grainCargoDisplayObject;
                break;

            case ResourceGatheringType.Wood:
                woodCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = woodCargoDisplayObject;
                break;

            case ResourceGatheringType.Stone:
                stoneCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = stoneCargoDisplayObject;
                break;

            case ResourceGatheringType.Gold:
                goldCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = goldCargoDisplayObject;
                break;
        }
    }

    public bool TryDropoff(Structure structure)
    {
        if (!structure || !HasCargo())
            return false;

        // ! Redundant, checked in the goal itself. Remove if no problems
        // ! arise from commenting out. Might still be needed in case of
        // ! building damage changes?
        // // if (!structure.CanDropOff(currentResource))
        // //     return false;

        //  Trigger a dropoff event
        DropoffEvent e = new DropoffEvent{ villager = this, structure = structure, resourceType = currentResource, amount = currentCargo };
        OnDropoffEvent?.Invoke(null, e);
        if (e.cancel) return false;   //  return if the event has been cancelled by any subscriber

        currentCargo -= e.amount;

        //  Send an indicator
        GameMaster.SendFloatingIndicator(structure.transform.position, $"+{(int)e.amount} {currentResource.ToString()}", Color.green);

        return true;
    }

    protected float GetWorkRate(ResourceGatheringType resourceType)
    {
        float rate = 0.0f;

        switch (resourceType)
        {
            case ResourceGatheringType.Grain:
                rate = rtsUnitTypeData.farmingRate;
                break;

            case ResourceGatheringType.Berries:
                rate = rtsUnitTypeData.foragingRate;
                break;

            case ResourceGatheringType.Wood:
                rate = rtsUnitTypeData.lumberjackingRate;
                break;

            case ResourceGatheringType.Stone:
                rate = rtsUnitTypeData.stoneMiningRate;
                break;

            case ResourceGatheringType.Gold:
                rate = rtsUnitTypeData.goldMiningRate;
                break;

            case ResourceGatheringType.Fish:
                rate = rtsUnitTypeData.fishingRate;
                break;

            case ResourceGatheringType.Meat:
                rate = rtsUnitTypeData.huntingRate;
                break;

            default:
                break;
        }

        rate = rate / (60/Constants.ACTOR_TICK_RATE);

        return rate;
    }

    public bool TryGather(Resource resource)
    {
        if (!resource || IsCargoFull())
            return false;

        //  Convert per second to per tick and clamp to how much cargo space we have
        float amount = GetWorkRate(resource.type);
        amount = Mathf.Clamp(rtsUnitTypeData.maxCargo - currentCargo, 0, amount);
        amount = resource.GetRemoveAmount(amount);

        //  Trigger a gather event
        GatherEvent e = new GatherEvent{ villager = this, resource = resource, resourceType = currentResource, amount = amount };
        OnGatherEvent?.Invoke(null, e);
        if (e.cancel) return false;   //  return if the event has been cancelled by any subscriber

        //  Remove from the resource and add to cargo
        amount = resource.TryRemove(e.amount);
        currentCargo += amount;

        // Animation
        switch(resource.type)
        {
            case ResourceGatheringType.Grain:
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.FARMING);
                break;

            case ResourceGatheringType.Berries:
            case ResourceGatheringType.Meat:
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.FORAGING);
                // // TODO: Clearing equipped items here for hunting
                // ChangeEquippedItems(null);
                break;

            case ResourceGatheringType.Fish:
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.FISHING);
                break;

            case ResourceGatheringType.Gold:
            case ResourceGatheringType.Stone:
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.MINING);
                break;

            case ResourceGatheringType.Wood:
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.LUMBERJACKING);
                break;
        }

        return true;
    }

    public bool TryHunt(Fauna fauna)
    {
        if (!fauna || IsCargoFull())
            return false;

        projectileTarget = fauna.gameObject;
        float amount = (rtsUnitTypeData.huntingDamage / (60 / Constants.ACTOR_TICK_RATE));
        fauna.AttributeHandler.Damage(amount, AttributeChangeCause.ATTACKED, AttributeHandler, DamageType.PIERCING);
        animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.HUNTING);

        return true;
    }

    public bool TryRepair(Structure structure)
    {
        if (!structure || !structure.NeedsRepairs())
            return false;

        //  Convert per second to per tick
        float amount = (rtsUnitTypeData.repairRate / (60/Constants.ACTOR_TICK_RATE));

        //  Trigger a repair event
        RepairEvent e = new RepairEvent{ villager = this, structure = structure, amount = amount };
        OnRepairEvent?.Invoke(null, e);
        if (e.cancel) return false;   //  return if the event has been cancelled by any subscriber

        structure.TryRepair(e.amount, this);

        if (!structure.NeedsRepairs())
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);
        else
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.BUILDANDREPAIR);

        return true;
    }

    public bool TryBuild(Constructible construction)
    {
        if (!construction)
            return false;

        //  Convert per second to per tick
        float amount = (rtsUnitTypeData.buildRate / (60/Constants.ACTOR_TICK_RATE));

        //  Trigger a build event
        BuildEvent e = new BuildEvent{ villager = this, constructible = construction, amount = amount };
        OnBuildEvent?.Invoke(null, e);
        if (e.cancel) return false;   //  return if the event has been cancelled by any subscriber

        construction.TryBuild(e.amount, this);

        if (construction.IsBuilt())
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);
        else
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.BUILDANDREPAIR);

        return true;
    }

    public void CleanupEvents()
    {
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;
        PathfindingGoal.OnGoalChangeEvent -= OnGoalChange;
        Damageable.OnDeathEvent -= OnDeath;
    }

    public void OnDestroy()
    {
        CleanupEvents();
    }

    public static event EventHandler<GatherEvent> OnGatherEvent;
    public class GatherEvent : Swordfish.Event
    {
        public Villager villager;
        public Resource resource;
        public ResourceGatheringType resourceType;
        public float amount;
    }

    public static event EventHandler<DropoffEvent> OnDropoffEvent;
    public class DropoffEvent : Swordfish.Event
    {
        public Villager villager;
        public Structure structure;
        public ResourceGatheringType resourceType;
        public float amount;
    }

    public static event EventHandler<RepairEvent> OnRepairEvent;
    public class RepairEvent : Swordfish.Event
    {
        public Villager villager;
        public Structure structure;
        public float amount;
    }

    public static event EventHandler<BuildEvent> OnBuildEvent;
    public class BuildEvent : Swordfish.Event
    {
        public Villager villager;
        public Constructible constructible;
        public float amount;
    }
}
