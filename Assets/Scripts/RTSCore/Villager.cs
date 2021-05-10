using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public enum UnitState
{
    IDLE,
    ROAMING,
    GATHERING,
    TRANSPORTING,
    BUILDANDREPAIR,
}

public class Villager : Unit
{
    [Header("AI")]
    public UnitState state;

    [Header("Villager")]
    public List<ResourceGatheringType> resourcePriorities;
    public ResourceGatheringType currentResource;    

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

    public bool IsCargoFull() { return currentCargo >= maxCargo; }
    public bool HasCargo() { return currentCargo > 0; }

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
        goals.Add<GoalTransportResource>();

        //  Assign priorities
        foreach (GoalGatherResource goal in goals.GetAll<GoalGatherResource>())
            resourcePriorities.Add(goal.type);
        
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
            Debug.Log("No audiosource component found.");

        animator = GetComponent<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");
    }

    public void OnDestroy()
    {
        CleanupEvents();
    }

    public override void Tick()
    {
        base.Tick();

        //  Transport type always matches what our current resource is
        // TODO: Should move this to a function that sets the state or
        // current resource desired to avoid calling it every frame.
        goals.Get<GoalTransportResource>().type = currentResource;

        //  Default to roaming if we cant find a goal
        // TODO: Probably should default to idle for performance reasons
        if (!GotoNearestGoalWithPriority() || !GotoNearestGoal())
            state = UnitState.ROAMING;
        
        switch (state)
        {
            case UnitState.ROAMING:
                Goto(
                    UnityEngine.Random.Range(gridPosition.x - 4, gridPosition.x + 4),
                    UnityEngine.Random.Range(gridPosition.x - 4, gridPosition.x + 4)
                );
                animator.SetInteger("VillagerActorState", (int)ActorAnimationState.LUMBERJACKING);
                break;

            case UnitState.GATHERING:
                if (IsCargoFull())  state = UnitState.TRANSPORTING;
                else if (!HasValidTarget()) state = UnitState.ROAMING;
            break;

            case UnitState.TRANSPORTING:
                if (!HasCargo()) state = UnitState.ROAMING;
                animator.SetInteger("VillagerActorState", (int)ActorAnimationState.MOVING);
            break;

            case UnitState.BUILDANDREPAIR:
                if (!HasValidTarget()) state = UnitState.IDLE;
            break;
            
            case UnitState.IDLE:
                animator.SetInteger("VillagerActorState", (int)ActorAnimationState.IDLE);
            break;
        }
    }

    public void CycleGatheringGoals()
    {
        //  Push the first priority to the end of the list
        resourcePriorities.Add(resourcePriorities[0]);
        resourcePriorities.RemoveAt(0);

        //  Reassign gathering goals
        int i = 0;
        foreach (GoalGatherResource goal in goals.GetAll<GoalGatherResource>())
            goal.type = resourcePriorities[i++];
    }

    public void OnRepathFailed(object sender, Actor.RepathFailedEvent e)
    {
        if (e.actor != this) return;

        //  If unable to find a path while gathering, reorder priorities
        //  It is likely we can't reach our current priority
        if (state == UnitState.GATHERING)
            CycleGatheringGoals();
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
                return;
            }
        }
        else if (e.goal is GoalTransportResource)
        {
            if (villager.HasCargo())
            {
                villager.state = UnitState.TRANSPORTING;
                return;
            }
        }
        else if (e.goal is GoalBuildRepair)
        {
            villager.state = UnitState.BUILDANDREPAIR;
        }

        //  default cancel the goal so that another can take priority
        ResetGoal();
        e.Cancel();
    }

    public void OnGoalInteract(object sender, PathfindingGoal.GoalInteractEvent e)
    {
        if (e.actor != this) return;

        Resource resource = e.cell.GetOccupant<Resource>();
        Villager villager = (Villager)e.actor;
        Structure structure = e.cell.GetOccupant<Structure>();

        if (e.goal is GoalGatherResource && !villager.IsCargoFull())
        {
            villager.TryGather(resource);
            return;
        }
        else if (e.goal is GoalTransportResource && villager.HasCargo())
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
        // These checks shouldn't be neccassary once we are past bug
        // squashing stages
        if (!structure) return;

        // Use the repair rate unless the building hasn't been constructed.
        int rate = repairRate;
        TerrainBuilding building = structure.GetComponent<TerrainBuilding>();
        if (building.NeedsBuilding())
        {
            rate = buildRate;
        }        
        // Repairing costs resources
        else
        {
            // TODO: Resource cost for repairing is hardcoded, should be
            // relative to building cost to build?
            PlayerManager.instance?.RemoveResources(1, 1, 1, 1);
        }    

        //  Convert per second to per tick and clamp to how much cargo space we have
        int amount = (int)(rate / (60/Constants.ACTOR_TICK_RATE));
        structure.GetComponent<TerrainBuilding>().RepairDamage(amount);
        
        // Use lumberjack animation
        animator.SetInteger("VillagerActorState", (int)ActorAnimationState.LUMBERJACKING);
    }

    public static event EventHandler<GatherEvent> OnGatherEvent;
    public class GatherEvent : Swordfish.Event
    {
        public Villager villager;
        public Resource resource;
        public int amount;
    }
}
