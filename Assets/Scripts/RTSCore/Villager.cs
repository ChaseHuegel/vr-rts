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
    public ResourceGatheringType currentResource;
    protected ResourceGatheringType previousResource;
    public GoalTransportResource transportGoal;

    // Should we have different cargo capacities based on resource
    // type? We use cargo as a generic container for resources and
    // the capacity changes on resource:
    // 1 cargo unit == 5 grain unit
    // 1 cargo unit == 3 wood units
    // 1 cargo unit == 2 stone units
    // 1 cargo unit == 1 gold unit
    // Could also use this system to get around the limits of our
    // currently used work rate, currently you can't go below a
    // work rate of 3 because the math puts the amount under 1 and
    // we're using INT's to store amount/capacity.
    public int maxCargo = 20;
    public float currentCargo = 0;
    public float workRate = 3;
    public float buildRate = 6;
    public float repairRate = 3;

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
    
    GameObject currentCargoDisplayObject;
    GameObject currentHandToolDisplayObject;
    public VillagerHoverMenu villagerHoverMenu;
    public bool IsCargoFull() { return currentCargo >= maxCargo; }
    public bool HasCargo() { return currentCargo > 0; }

    public void HookIntoEvents()
    {
        AttributeHandler.OnDamageEvent += OnDamage;
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;
        PathfindingGoal.OnGoalChangeEvent += OnGoalChange;
        Damageable.OnDeathEvent += OnDeath;
    }

    public void CleanupEvents()
    {
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;
        PathfindingGoal.OnGoalChangeEvent -= OnGoalChange;
        Damageable.OnDeathEvent -= OnDeath;
    }


    public override void Initialize()
    {
        base.Initialize();
        HookIntoEvents();

        SetUnitType(rtsUnitType);

        if(PlayerManager.instance.factionID == factionID)
            PlayerManager.instance.AddToPopulation((Unit)this);
       

        //ChangeTaskVisuals();

    }

    public void OnDamage(object sender, Damageable.DamageEvent e)
    {
    }

    public void OnDeath(object sender, Damageable.DeathEvent e)
    {
        if (e.victim != this)
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

    public void OnDestroy()
    {        
        CleanupEvents();
    }

    public bool TaskChanged()
    {
        return StateChanged() && previousResource != currentResource;
    }

    bool StateChanged()
    {
        return state != previousState;
    }

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

    float detachFromHandTime;

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

        Structure building = collision.gameObject.GetComponentInParent<Structure>();
        if (building)
        {
            SetUnitType(RTSUnitType.Builder);
            ResetAI();
        }
    }


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
                // Goto(
                //     UnityEngine.Random.Range(gridPosition.x - 4, gridPosition.x + 4),
                //     UnityEngine.Random.Range(gridPosition.x - 4, gridPosition.x + 4)
                // );
                // ChangeTaskVisuals();
            break;

            case UnitState.GATHERING:
                //if (IsCargoFull()) state = UnitState.IDLE;
            break;

            case UnitState.TRANSPORTING:
                //if (!HasCargo()) state = UnitState.IDLE;
            break;

            case UnitState.BUILDANDREPAIR:
            break;

            case UnitState.IDLE:
                //animator.SetInteger("VillagerActorState", (int)ActorAnimationState.IDLE);
            break;
        }

        //Debug.Log("State: " + state.ToString() + " PreviousState: " + previousState.ToString());

        if (IsMoving() )
        {
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.MOVING);
        }

        if (TaskChanged())
        {
            ChangeEquippedItems();
            //PlayChangeTaskAudio();
        }

        previousState = state;
        previousResource = currentResource;
    }

     // Used by animator to play sound effects
    public void AnimatorPlayAudio(string clipName)
    {
        AudioSource.PlayClipAtPoint(GameMaster.GetAudio(clipName).GetClip(), transform.position, 0.75f);
    }

    // TODO: Should this be part of the unit base class to be
    // overridden by inheritors? Should unitType be changed to
    // unitTask or unitJob?
    public override void SetUnitType(RTSUnitType unitType)
    {
        base.SetUnitType(unitType);
        // SetUnitData(GameMaster.Instance.unitDatabase.Get(unitType));

        // Turn off all goals except the transport goal.
        goals.Clear();
        transportGoal = goals.Add<GoalTransportResource>();

        switch ( unitType )
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
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Meat;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Fish;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Gold;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Stone;
                goals.Add<GoalGatherResource>().type = ResourceGatheringType.Wood;
                break;

            default:
                break;
        }

        PlayChangeTaskAudio();
    }

    public void PlayChangeTaskAudio()
    {
        if (state == UnitState.GATHERING)
        {
            switch (currentResource)
            {
                case ResourceGatheringType.Gold:
                case ResourceGatheringType.Stone:
                    audioSource.clip = GameMaster.GetAudio("miner").GetClip();
                    audioSource.Play();
                    break;

                case ResourceGatheringType.Grain:
                case ResourceGatheringType.Berries:
                case ResourceGatheringType.Meat:
                case ResourceGatheringType.Fish:
                    audioSource.clip = GameMaster.GetAudio("farmer").GetClip();
                    audioSource.Play();
                    break;

                case ResourceGatheringType.Wood:
                    audioSource.clip = GameMaster.GetAudio("lumberjack").GetClip();
                    audioSource.Play();
                    break;

                default:
                    break;
            }
        }
        else if (state == UnitState.BUILDANDREPAIR)
        {
            audioSource.clip = GameMaster.GetAudio("builder").GetClip();
            audioSource.Play();
        }
    }

    public void ChangeEquippedItems(ResourceGatheringType resourceType = ResourceGatheringType.None)
    {
        if (currentHandToolDisplayObject)
            currentHandToolDisplayObject.SetActive(false);

        if (resourceType == ResourceGatheringType.None)
            resourceType = currentResource;

        if (state == UnitState.GATHERING)
        {
            switch (resourceType)
            {
                case ResourceGatheringType.Gold:
                    goldHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = goldHandToolDisplayObject;
                    break;

                case ResourceGatheringType.Stone:
                    stoneHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = stoneHandToolDisplayObject;
                    break;

                case ResourceGatheringType.Grain:
                    grainHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = grainHandToolDisplayObject;
                    break;

                case ResourceGatheringType.Berries:
                    // Equip nothing.
                    break;

                case ResourceGatheringType.Meat:
                    hunterHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = hunterHandToolDisplayObject;
                    break;

                case ResourceGatheringType.Fish:
                    fishermanHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = fishermanHandToolDisplayObject;
                    break;

                case ResourceGatheringType.Wood:
                    woodHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = woodHandToolDisplayObject;
                    break;

                default:
                    break;
            }
        }
        else if (state == UnitState.BUILDANDREPAIR)
        {
            builderHandToolDisplayObject.SetActive(true);
            currentHandToolDisplayObject = builderHandToolDisplayObject;
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
            {
                grainCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = grainCargoDisplayObject;
                break;
            }

            case ResourceGatheringType.Wood:
            {
                woodCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = woodCargoDisplayObject;
                break;
            }

            case ResourceGatheringType.Stone:
            {
                stoneCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = stoneCargoDisplayObject;
                break;
            }

            case ResourceGatheringType.Gold:
            {
                goldCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = goldCargoDisplayObject;
                break;
            }
        }
    }

    PathfindingGoal currentGoalFound;
    PathfindingGoal previousGoalFound;

    public void OnGoalChange(object sender, PathfindingGoal.GoalChangeEvent e)
    {
        if (e.actor != this) return;

        if (HasTargetChanged())
        {
            Resource resource = previousGoalTarget?.GetFirstOccupant<Resource>();
            if (resource) resource.interactors--;
        }

        ChangeEquippedItems();
    }

    public void OnGoalFound(object sender, PathfindingGoal.GoalFoundEvent e)
    {
        if (e.actor != this) return;

        Villager villager = (Villager)e.actor;

        // if (e.cell != previousGoalTarget)
        // {
        //     Resource resource = previousGoalTarget?.GetFirstOccupant<Resource>();
        //     if (resource) resource.interactors--;
        // }

        //  Need C# 7 in Unity for switching by type!!!
        if (e.goal is GoalGatherResource && !villager.IsCargoFull())
        {
            villager.state = UnitState.GATHERING;
            villager.currentResource = ((GoalGatherResource)e.goal).type;
            currentGoalFound = e.goal;
            DisplayCargo(false);

            Resource resource = e.cell?.GetFirstOccupant<Resource>();

            if (!resource.IsBusy())
            {
                resource.interactors++;
                return;
            }
        }
        else if (e.goal is GoalTransportResource && villager.HasCargo())
        {
            villager.state = UnitState.TRANSPORTING;
            currentGoalFound = e.goal;
            DisplayCargo(true);
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

        if  (e.goal is GoalGatherResource && villager.TryGather(resource) ||
            (e.goal is GoalTransportResource && villager.TryDropoff(structure) ||
            (e.goal is GoalBuildRepair && (villager.TryRepair(structure) || villager.TryBuild(construction)))))
        {
            return;
        }

        //  default cancel the interaction
        ResetGoal();
        e.Cancel();
    }

    public bool TryDropoff(Structure structure)
    {
        if (!structure || !HasCargo())
            return false;

        if (structure.factionID != factionID)
            return false;

        if (!structure.CanDropOff(currentResource))
            return false;

        //  Trigger a dropoff event
        DropoffEvent e = new DropoffEvent{ villager = this, structure = structure, resourceType = currentResource, amount = currentCargo };
        OnDropoffEvent?.Invoke(null, e);
        if (e.cancel) return false;   //  return if the event has been cancelled by any subscriber

        currentCargo -= e.amount;

        // Moved to static event handler OnVillagerDropoff in PlayerManager
        //PlayerManager.instance.AddResourceToStockpile(currentResource, (int)e.amount);

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
        amount = Mathf.Clamp(maxCargo - currentCargo, 0, amount);
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
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.FORAGING);
            break;

            case ResourceGatheringType.Meat:
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.HUNTING);
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

    public bool TryRepair(Structure structure)
    {
        if (!structure || !structure.NeedsRepairs())
            return false;

        if (structure.factionID != factionID)
            return false;

        //  Convert per second to per tick
        float amount = (rtsUnitTypeData.repairRate / (60/Constants.ACTOR_TICK_RATE));

        //  Trigger a repair event
        RepairEvent e = new RepairEvent{ villager = this, structure = structure, amount = amount };
        OnRepairEvent?.Invoke(null, e);
        if (e.cancel) return false;   //  return if the event has been cancelled by any subscriber

        structure.TryRepair(e.amount, this);

        // Use lumberjack animation
        animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.BUILDANDREPAIR);

        return true;
    }

    public bool TryBuild(Constructible construction)
    {
        if (!construction || construction.IsBuilt())
            return false;

        if (construction.factionID != factionID)
            return false;
            
        //  Convert per second to per tick
        float amount = (rtsUnitTypeData.buildRate / (60/Constants.ACTOR_TICK_RATE));

        //  Trigger a build event
        BuildEvent e = new BuildEvent{ villager = this, constructible = construction, amount = amount };
        OnBuildEvent?.Invoke(null, e);
        if (e.cancel) return false;   //  return if the event has been cancelled by any subscriber

        construction.TryBuild(e.amount, this);

        // Use lumberjack animation
        animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.BUILDANDREPAIR);

        return true;
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
