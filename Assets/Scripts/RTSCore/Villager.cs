using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;
using Valve.VR.InteractionSystem;

public enum UnitState
{
    IDLE,
    ROAMING,
    GATHERING,
    TRANSPORTING,
    BUILDANDREPAIR,
}

[RequireComponent(typeof(Damageable))]
public class Villager : Unit
{
    [Header("AI")]
    public UnitState state;
    protected UnitState previousState;

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
    public int currentCargo = 0;
    public int workRate = 3;
    public int buildRate = 6;
    public int repairRate = 3;
    protected Animator animator;
    private AudioSource audioSource;

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
    GameObject currentCargoDisplayObject;
    GameObject currentHandToolDisplayObject;
   public VillagerHoverMenu villagerHoverMenu;
    public bool IsCargoFull() { return currentCargo >= maxCargo; }
    public bool HasCargo() { return currentCargo > 0; }

    bool isHeld;

    public void HookIntoEvents()
    {
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;

        Actor.OnRepathFailedEvent += OnRepathFailed;
    }

    public void CleanupEvents()
    {
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;

        Actor.OnRepathFailedEvent -= OnRepathFailed;
    }

    public override void Initialize()
    {
        base.Initialize();

        HookIntoEvents();

        //  Add goals in order of priority
        goals.Add<GoalBuildRepair>();
        goals.Add<GoalGatherResource>().type = ResourceGatheringType.Grain;
        goals.Add<GoalGatherResource>().type = ResourceGatheringType.Gold;
        goals.Add<GoalGatherResource>().type = ResourceGatheringType.Stone;
        goals.Add<GoalGatherResource>().type = ResourceGatheringType.Wood;
        transportGoal = goals.Add<GoalTransportResource>();
       
        audioSource = gameObject.GetComponent<AudioSource>();
        if (!audioSource)
            Debug.Log("No audiosource component found.");

        animator = gameObject.GetComponentInChildren<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");

        PlayerManager.instance.AddToPopulation(rtsUnitTypeData.unitType);

        //ChangeTaskVisuals();
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

    public void OnHandHoverBegin(Hand hand)
    {
        villagerHoverMenu.Show();
    }

    public void OnHandHoverEnd(Hand hand)
    {
        villagerHoverMenu.Hide();
    }

    public void OnAttachedToHand(Hand hand)
    {
        isHeld = true;
        villagerHoverMenu.Show();    
        Freeze();                    
        animator.SetInteger("VillagerActorState", 0);
        //audioSource.PlayOneShot(GameMaster.GetAudio("unitPickup").GetClip(), 0.5f);
    }

    public void OnDetachedFromHand(Hand hand)
    {        
        Debug.Log("det");
        isHeld = false;
        villagerHoverMenu.Hide();
        ResetPathing();
        Unfreeze();        
        // audioSource.Stop();
    }

    public override void Tick()
    {
        if (isHeld)
            return;

        base.Tick();

        //  Transport type always matches what our current resource is
        transportGoal.type = currentResource;

        //  Default to roaming if we cant find a goal
        // TODO: Probably should default to idle for performance reasons
        if (!GotoNearestGoalWithPriority())
            state = UnitState.ROAMING;

        switch (state)
        {
            case UnitState.ROAMING:
                Goto(
                    UnityEngine.Random.Range(gridPosition.x - 4, gridPosition.x + 4),
                    UnityEngine.Random.Range(gridPosition.x - 4, gridPosition.x + 4)
                );
                if (IsMoving())
                    animator.SetInteger("VillagerActorState", (int)ActorAnimationState.MOVING);
                ChangeTaskVisuals(); 
            break;

            case UnitState.GATHERING:
                if (IsCargoFull())  state = UnitState.TRANSPORTING;
                else if (!HasValidTarget()) state = UnitState.ROAMING;
                if (IsMoving())
                    animator.SetInteger("VillagerActorState", (int)ActorAnimationState.MOVING);
            break;

            case UnitState.TRANSPORTING:
                if (!HasCargo()) state = UnitState.ROAMING;
                if (IsMoving())
                    animator.SetInteger("VillagerActorState", (int)ActorAnimationState.MOVING);                
            break;

            case UnitState.BUILDANDREPAIR:
                if (!HasValidTarget()) state = UnitState.IDLE;
                if (IsMoving())
                    animator.SetInteger("VillagerActorState", (int)ActorAnimationState.MOVING);                
            break;

            case UnitState.IDLE:
                animator.SetInteger("VillagerActorState", (int)ActorAnimationState.IDLE);
            break;
        }

        if (TaskChanged())
        {
            ChangeTaskVisuals();
            //PlayChangeTaskAudio();
        }

        previousState = state;
        previousResource = currentResource;
    }

    // This is is used to reenable the character after they have been
    // released from the hand AND after they have landed somewhere.
    private void OnCollisionEnter(Collision collision)
    {
        //this.enabled = true;
        //Unfreeze();
        //audioSource.Stop();

        // Resource resource = collision.gameObject.GetComponent<Resource>();
        // if (resource)
        // {
        //     ResetPathing();
        //     currentGoalTarget = GetCellAtTransform();
        //     // currentGoal = (PathfindingGoal)Get goals.Get<GoalGatherResource>(x => ((GoalGatherResource)x).type == resource.type);
            
        //     if (GotoNearestGoal()) //currentGoal != null && currentGoalTarget != null)
        //         PlayTaskChangeAudio(resource.type);
            
        //     return;
        // }
    }

    public void OnRepathFailed(object sender, Actor.RepathFailedEvent e)
    {
        if (e.actor != this) return;

        //  If unable to find a path, reorder priorities
        //  It is likely we can't reach our current goal
        //  This gives a simple decision making behavior
        if (state == UnitState.GATHERING)
            goals.Cycle();
    }

    // Used by animator to play sound effects
    public void AnimatorPlayAudio(string clipName) 
    {
        AudioSource.PlayClipAtPoint(GameMaster.GetAudio(clipName).GetClip(), transform.position, 0.75f);
    }

    // TODO: Should this be part of the unit base class to be 
    // overridden by inheritors? Should unitType be changed to
    // unitTask or unitJob?
    public void SetRTSUnitType(RTSUnitType rtsUnitType)    
    {
        switch ( rtsUnitType )
        {
            case RTSUnitType.Builder:
                state = UnitState.BUILDANDREPAIR;
                currentResource = ResourceGatheringType.None;
                ResetPathing();
                break;

            case RTSUnitType.Farmer:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Grain;
                ResetPathing();
                break;

            case RTSUnitType.Lumberjack:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Wood;
                ResetPathing();
                break;

            case RTSUnitType.GoldMiner:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Gold;
                ResetPathing();
                break;

            case RTSUnitType.StoneMiner:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Stone;
                ResetPathing();
                break;

            default:
                break;
        }
    }

    public void PlayChangeTaskAudio()
    {
        if (state == UnitState.GATHERING)
        {
            switch (currentResource)
            {
                case ResourceGatheringType.Gold:
                case ResourceGatheringType.Stone:
                    audioSource.PlayOneShot(GameMaster.GetAudio("miner").GetClip());
                    break;

                case ResourceGatheringType.Grain:
                    audioSource.PlayOneShot(GameMaster.GetAudio("farmer").GetClip());
                    break;

                case ResourceGatheringType.Wood:
                    audioSource.PlayOneShot(GameMaster.GetAudio("lumberjack").GetClip());
                    break;

                default:
                    break;
            }
        }
        else if (state == UnitState.BUILDANDREPAIR)
        {
            audioSource.PlayOneShot(GameMaster.GetAudio("builder").GetClip());
        }
    }

    public void ChangeTaskVisuals(ResourceGatheringType resourceType = ResourceGatheringType.None)
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
                    //handGrainDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = null;// handGrainDisplayObject;
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

    public void OnGoalFound(object sender, PathfindingGoal.GoalFoundEvent e)
    {
        if (e.actor != this) return;

        Villager villager = (Villager)e.actor;

        //  Need C# 7 in Unity for switching by type!!!
        if (e.goal is GoalGatherResource)
        {
            if (!villager.IsCargoFull())
            {
                villager.state = UnitState.GATHERING;
                villager.currentResource = ((GoalGatherResource)e.goal).type;
                DisplayCargo(false);
                return;
            }
        }
        else if (e.goal is GoalTransportResource)
        {
            if (villager.HasCargo())
            {
                villager.state = UnitState.TRANSPORTING;
                DisplayCargo(true);
                return;
            }
        }
        else if (e.goal is GoalBuildRepair)
        {
            villager.state = UnitState.BUILDANDREPAIR;
            return;
        }

        //  default cancel the goal so that another can take priority
        ResetGoal();
        e.Cancel();
    }

    public void OnGoalInteract(object sender, PathfindingGoal.GoalInteractEvent e)
    {
        if (e.actor != this) return;

        Villager villager = (Villager)e.actor;
        Resource resource = e.cell.GetOccupant<Resource>();
        Structure structure = e.cell.GetOccupant<Structure>();

        if (e.goal is GoalGatherResource && !villager.IsCargoFull())
        {
            villager.TryGather(resource);
            return;
        }
        else if (e.goal is GoalTransportResource && villager.HasCargo() && structure.IsBuilt())
        {
            PlayerManager.instance?.AddResourceToStockpile(villager.currentResource, villager.currentCargo);
            villager.currentCargo = 0;
            return;
        }
        else if (e.goal is GoalBuildRepair)
        {
            villager.TryBuildRepair(structure);
            return;
        }

        //  default cancel the interaction
        ResetGoal();
        e.Cancel();
    }

    public void TryGather(Resource resource)
    {
        // These checks shouldn't be neccassary once we are past bug
        // squashing stages
        if (!resource) return;

        //  Convert per second to per tick and clamp to how much cargo space we have
        float amount = (workRate / (60/Constants.ACTOR_TICK_RATE));
        amount = Mathf.Clamp(maxCargo - currentCargo, 0, amount);
        amount = resource.GetRemoveAmount((int)amount);

        //  Trigger a gather event
        GatherEvent e = new GatherEvent{ villager = this, resource = resource, amount = (int)amount };
        OnGatherEvent?.Invoke(null, e);
        if (e.cancel == true) { return; }   //  return if the event has been cancelled by any subscriber

        //  Remove from the resource and add to cargo
        amount = resource.TryRemove(e.amount);
        currentCargo += (int)amount;

        // Animation
        switch(resource.type)
        {
            case ResourceGatheringType.Grain:
                animator.SetInteger("VillagerActorState", (int)ActorAnimationState.FARMING);
            break;

            case ResourceGatheringType.Gold:
            case ResourceGatheringType.Stone:
                animator.SetInteger("VillagerActorState", (int)ActorAnimationState.MINING);
            break;

            case ResourceGatheringType.Wood:
                animator.SetInteger("VillagerActorState", (int)ActorAnimationState.LUMBERJACKING);
            break;
        }
    }

    public void TryBuildRepair(Structure structure)
    {
        // These checks shouldn't be necassary once we are past bug
        // squashing stages
        if (!structure) return;

        // Use the repair rate unless the building hasn't been constructed.
        int rate = repairRate;

        if (!structure.IsBuilt())
        {
            rate = buildRate;
        }
        // Repairing costs resources
        else
        {
            // TODO: Resource cost for repairing is hardcoded, should be
            // relative to building cost to build?
            PlayerManager.instance?.RemoveResourcesFromStockpile(1, 1, 1, 1);
        }

        //  Convert per second to per tick
        int amount = (int)(rate / (60/Constants.ACTOR_TICK_RATE));
        structure.TryRepair(amount, this);

        // Use lumberjack animation
        animator.SetInteger("VillagerActorState", (int)ActorAnimationState.BUILDANDREPAIR);
    }

    public static event EventHandler<GatherEvent> OnGatherEvent;
    public class GatherEvent : Swordfish.Event
    {
        public Villager villager;
        public Resource resource;
        public int amount;
    }
}
