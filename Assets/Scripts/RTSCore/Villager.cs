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

        //  Add goals in order of priority
        goals.Add<GoalWoodCutting>();
        goals.Add<GoalTransportWood>();
        goals.Add<GoalGoldMining>();
        goals.Add<GoalTransportGold>();
        goals.Add<GoalStoneMining>();
        goals.Add<GoalTransportStone>();
        goals.Add<GoalFarming>();
        goals.Add<GoalTransportGrain>();

        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
            Debug.Log("No audiosource component found.");

        animator = GetComponent<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");
    }

    public override void Tick()
    {
        base.Tick();

        //  Woodcutting should not be active if cargo is full
        goals.Get<GoalWoodCutting>().active = !IsCargoFull();

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
                if (IsCargoFull()) state = UnitState.TRANSPORTING;
                if (!HasValidTarget()) state = UnitState.ROAMING;
            break;

            case UnitState.TRANSPORTING:
                if (!HasCargo()) state = UnitState.ROAMING;
            break;
        }
    }

    public void TryGather(Resource resource)
    {
        if (!resource) return;

        //  Convert per second to per tick and clamp to how much cargo space we have
        int amount = (int)(workRate / (60/Constants.ACTOR_TICK_RATE));
        amount = Mathf.Clamp(maxCargo - currentCargo, 0, amount);
        amount = resource.GetRemoveAmount(amount);

        //  Trigger a gather event
        GatherEvent e = new GatherEvent{ target = resource, amount = amount };
        OnGatherEvent?.Invoke(this, e);
        if (e.cancel == true) { return; }   //  return if the event has been cancelled by any subscriber

        //  Remove from the resource and add to cargo
        amount = resource.TryRemove(e.amount);
        currentCargo += amount;

        animator.Play("Lumberjacking", -1, 0);
    }

    public event EventHandler<GatherEvent> OnGatherEvent;
    public class GatherEvent : Swordfish.Event
    {
        public Resource target;
        public int amount;
    }
}
