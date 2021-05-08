using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public enum UnitState
{
    ROAMING,
    GATHERING,
    TRANSPORTING
}

public class Villager : Unit
{
    [Header("AI")]
    public UnitState state;

    [Header("Villager")]
    public ResourceGatheringType currentResource;
    public int workRate = 10;
    public int maxCargo = 100;
    public int currentCargo = 0;

    protected Animator animator;
    private AudioSource audioSource;

    public bool IsCargoFull() { return currentCargo >= maxCargo; }
    public bool HasCargo() { return currentCargo > 0; }

    public override void Initialize()
    {
        base.Initialize();

        //  Subscribe to events. They are static, make sure to unsubscribe!
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;

        //  Add goals in order of priority
        goals.Add<GoalGatherResource>().type = ResourceGatheringType.Grain;
        goals.Add<GoalGatherResource>().type = ResourceGatheringType.Gold;
        goals.Add<GoalGatherResource>().type = ResourceGatheringType.Stone;
        goals.Add<GoalGatherResource>().type = ResourceGatheringType.Wood;
        goals.Add<GoalTransportResource>();

        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
            Debug.Log("No audiosource component found.");

        animator = GetComponent<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");
    }

    public void OnDestroy()
    {
        //  Unsubscribe from events
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;
    }

    public override void Tick()
    {
        base.Tick();

        //  Transport type always matches what our current resource is
        goals.Get<GoalTransportResource>().type = currentResource;

        //  Default to roaming if we cant find a goal
        if (!GotoNearestGoalWithPriority())
            state = UnitState.ROAMING;

        switch (state)
        {
            case UnitState.ROAMING:
                Goto(
                    UnityEngine.Random.Range(gridPosition.x - 4, gridPosition.x + 4),
                    UnityEngine.Random.Range(gridPosition.x - 4, gridPosition.x + 4)
                );
                break;

            case UnitState.GATHERING:
                if (IsCargoFull())  state = UnitState.TRANSPORTING;
                else if (!HasValidTarget()) state = UnitState.ROAMING;
            break;

            case UnitState.TRANSPORTING:
                if (!HasCargo()) state = UnitState.ROAMING;
            break;
        }
    }

    public static void OnGoalFound(object sender, PathfindingGoal.GoalFoundEvent e)
    {
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

        e.Cancel();    //  default cancel the goal so that another can take priority
    }

    public static void OnGoalInteract(object sender, PathfindingGoal.GoalInteractEvent e)
    {
        Resource resource = e.cell.GetOccupant<Resource>();
        Villager villager = (Villager)e.actor;

        if (e.goal is GoalGatherResource)
        {
            villager.TryGather(resource);
            return;
        }

        else if (e.goal is GoalTransportResource)
        {
            PlayerManager.instance?.AddResourceToStockpile(villager.currentResource, villager.currentCargo);
            villager.currentCargo = 0;
            return;
        }

        e.Cancel();    //  default cancel the interaction
    }

    public void TryGather(Resource resource)
    {
        if (!resource) return;

        //  Convert per second to per tick and clamp to how much cargo space we have
        int amount = (int)(workRate / (60/Constants.ACTOR_TICK_RATE));
        amount = Mathf.Clamp(maxCargo - currentCargo, 0, amount);
        amount = resource.GetRemoveAmount(amount);

        //  Trigger a gather event
        GatherEvent e = new GatherEvent{ villager = this, resource = resource, amount = amount };
        OnGatherEvent?.Invoke(null, e);
        if (e.cancel == true) { return; }   //  return if the event has been cancelled by any subscriber

        //  Remove from the resource and add to cargo
        amount = resource.TryRemove(e.amount);
        currentCargo += amount;

        animator.Play("Lumberjacking", -1, 0);
    }

    public static event EventHandler<GatherEvent> OnGatherEvent;
    public class GatherEvent : Swordfish.Event
    {
        public Villager villager;
        public Resource resource;
        public int amount;
    }
}
