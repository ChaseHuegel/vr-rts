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
    public VillagerHoverMenu handCommandMenu;
    protected PathfindingGoal currentGoalFound;
    protected PathfindingGoal previousGoalFound;
    public bool IsCargoFull() { return currentCargo >= rtsUnitTypeData.maxCargo; }
    public bool HasCargo() { return currentCargo > 0; }

    public override void Initialize()
    {
        base.Initialize();
        HookIntoEvents();

        goals.Add<GoalGatherStone>();
        goals.Add<GoalBuildRepair>();
        goals.Add<GoalGatherGrain>();
        goals.Add<GoalGatherBerries>();
        goals.Add<GoalGatherFish>();
        goals.Add<GoalHuntFauna>();
        goals.Add<GoalGatherMeat>();
        goals.Add<GoalGatherWood>();
        goals.Add<GoalGatherGold>();
        transportGoal = goals.Add<GoalTransportResource>();

        ChangeVillagerType(rtsUnitType);
    }

    public void HookIntoEvents()
    {
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;
        PathfindingGoal.OnGoalChangeEvent += OnGoalChange;
        Damageable.OnDeathEvent += OnDeath;
        AttributeHandler.OnDamageEvent += OnDamaged;
    }

    public void CleanupEvents()
    {
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;
        PathfindingGoal.OnGoalChangeEvent -= OnGoalChange;
        Damageable.OnDeathEvent -= OnDeath;
        AttributeHandler.OnDamageEvent -= OnDamaged;
    }

#region Hand Events

    public override void OnHandHoverBegin(Hand hand)
    {
        base.OnHandHoverBegin(hand);
        handCommandMenu.Show();
    }

    public override void OnHandHoverEnd(Hand hand)
    {
        base.OnHandHoverEnd(hand);
        handCommandMenu.Hide();
    }

    public override void OnAttachedToHand(Hand hand)
    {
        base.OnAttachedToHand(hand);
        handCommandMenu.Show();
    }

    public override void OnDetachedFromHand(Hand hand)
    {
        base.OnDetachedFromHand(hand);
        handCommandMenu.Hide();
    }

#endregion

#region Event Handlers

    public void OnDamaged(object sender, Damageable.DamageEvent e)
    {
        if (e.victim != AttributeHandler)
            return;

        if (!IsMoving())
        {
            //DeactivateAllGoals();
            GotoForced(UnityEngine.Random.Range(-5, 5) + gridPosition.x,
               UnityEngine.Random.Range(-5, 5) + gridPosition.y);
            LockPath();
        }
    }

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
    void OnTriggerEnter(Collider collider)
    {
        if (!wasThrownOrDropped)
            return;

        // TODO: could just switch this to a cell lookup where
        // TODO: they land.
        // Don't wait for a collision indefinitely.
        if (Time.time - detachFromHandTime >= 2.0f)
        {
            wasThrownOrDropped = false;
            return;
        }

        Unfreeze();

        Resource resource = collider.gameObject.GetComponent<Resource>();
        if (resource)
        {
            AssignUnitToResourceTask(resource);
            return;
        }

        Fauna fauna = collider.gameObject.GetComponent<Fauna>();
        if (fauna)
        {
            AssignUnitToFaunaTask(fauna);
            return;
        }

        Structure structure = collider.gameObject.GetComponentInParent<Structure>();
        if (structure)
        {
            AssignUnitToStructureTask(structure);
            return;
        }

        Constructible constructible = collider.gameObject.GetComponentInParent<Constructible>();
        if (constructible)
        {
            AssignUnitToConstructibleTask(constructible);
            return;
        }
    }

    // This is is used to reenable the character after they have been
    // released from the hand AND after they have landed somewhere.
    public override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    public override bool IsCivilian() { return true; }

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

        if (e.cell != previousGoalCell)
        {
            Resource resource = previousGoalCell?.GetFirstOccupant<Resource>();
            if (resource) resource.RemoveInteractor(this);
        }

        maxGoalInteractRange = rtsUnitTypeData.attackRange;
        DisplayCargo(false);

        //  Need C# 7 in Unity for switching by type!!!
        if (e.goal is GoalGatherBerries && !IsCargoFull())
        {
            state = UnitState.GATHERING;
            currentResource = ResourceGatheringType.Berries;
            currentGoalFound = e.goal;

            Resource resource = e.cell?.GetFirstOccupant<Resource>();
            if (resource.AddInteractor(this))
                return;
        }
        else if (e.goal is GoalGatherFish && !IsCargoFull())
        {
            state = UnitState.GATHERING;
            currentResource = ResourceGatheringType.Fish;
            currentGoalFound = e.goal;

            Resource resource = e.cell?.GetFirstOccupant<Resource>();
            if (resource.AddInteractor(this))
                return;
        }
        else if (e.goal is GoalGatherGold && !IsCargoFull())
        {
            state = UnitState.GATHERING;
            currentResource = ResourceGatheringType.Gold;
            currentGoalFound = e.goal;

            Resource resource = e.cell?.GetFirstOccupant<Resource>();
            if (resource.AddInteractor(this))
                return;
        }
        else if (e.goal is GoalGatherGrain && !IsCargoFull())
        {
            state = UnitState.GATHERING;
            currentResource = ResourceGatheringType.Grain;
            currentGoalFound = e.goal;

            Resource resource = e.cell?.GetFirstOccupant<Resource>();
            if (resource.AddInteractor(this))
                return;
        }
        else if (e.goal is GoalGatherMeat && !IsCargoFull())
        {
            state = UnitState.GATHERING;
            currentResource = ResourceGatheringType.Meat;
            currentGoalFound = e.goal;

            Resource resource = e.cell?.GetFirstOccupant<Resource>();
            if (resource.AddInteractor(this))
                return;
        }
        else if (e.goal is GoalGatherStone && !IsCargoFull())
        {
            state = UnitState.GATHERING;
            currentResource = ResourceGatheringType.Stone;
            currentGoalFound = e.goal;

            Resource resource = e.cell?.GetFirstOccupant<Resource>();
            if (resource.AddInteractor(this))
                return;
        }
        else if (e.goal is GoalGatherWood && !IsCargoFull())
        {
            state = UnitState.GATHERING;
            currentResource = ResourceGatheringType.Wood;
            currentGoalFound = e.goal;

            Resource resource = e.cell?.GetFirstOccupant<Resource>();
            if (resource.AddInteractor(this))
                return;
        }
        else if (e.goal is GoalTransportResource && HasCargo())
        {
            state = UnitState.TRANSPORTING;
            currentGoalFound = e.goal;
            DisplayCargo(true);
            return;
        }
        else if (e.goal is GoalHuntFauna && !IsCargoFull())
        {
            maxGoalInteractRange = GameMaster.GetUnit(RTSUnitType.Hunter).attackRange;
            state = UnitState.GATHERING;
            currentGoalFound = e.goal;
            return;
        }
        else if (e.goal is GoalBuildRepair)
        {
            state = UnitState.BUILDANDREPAIR;
            currentGoalFound = e.goal;
            return;
        }
        else if (e.goal is GoalGotoLocation)
        {
            GoalGotoLocation g = (GoalGotoLocation)e.goal;
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

        Resource resource = e.cell.GetOccupant<Resource>();
        Structure structure = e.cell.GetOccupant<Structure>();
        Constructible construction = e.cell.GetOccupant<Constructible>();
        Fauna fauna = e.cell.GetOccupant<Fauna>();

        if  (e.goal is GoalHuntFauna && TryHunt(fauna) ||
            e.goal is GoalGatherBerries && TryGather(resource) ||
            e.goal is GoalGatherFish && TryGather(resource) ||
            e.goal is GoalGatherGold && TryGather(resource) ||
            e.goal is GoalGatherGrain && TryGather(resource) ||
            e.goal is GoalGatherMeat && TryGather(resource) ||
            e.goal is GoalGatherStone && TryGather(resource) ||
            e.goal is GoalGatherWood && TryGather(resource) ||
            e.goal is GoalTransportResource && TryDropoff(structure) ||
            e.goal is GoalBuildRepair && (TryRepair(structure) ||
            TryBuild(construction)))
        {
            return;
        }
        else if (e.goal is GoalGotoLocation)
        {
            ActivateAllGoals();
            e.goal.active = false;

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

        // Transport type always matches what our current resource is
        // ! Should be able to move this somewhere so that it's not called
        // ! every tick.
        if (transportGoal != null) transportGoal.type = currentResource;

        GotoNearestGoalWithPriority();

        if (IsMoving() )
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.MOVING);
        else if (IsIdle())
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);

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

    /// <summary>
    /// Sets a unit to a specific task to the exclusion of all others by
    /// deactivating all goals except the desired goal and a transport
    /// goal. The exception to this is the drifter unit type which has
    /// all goals active. Also attempts to set the goal of the unit to the
    /// location passed in.
    /// </summary>
    /// <param name="unitType">The units new job/task.</param>
    /// <param name="taskLocation">The transform space location of the task.</param>
    public override void AssignUnitTaskAndLocation(RTSUnitType unitType, Cell taskLocation = null)
    {
        ChangeVillagerType(unitType);

        animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);

        if (taskLocation != null)
        {
            TrySetGoal(taskLocation);   

            // START: 
            // Find a path to the task location and go to the task location. The goal is to 
            // move/travel and should be their sole focus until they reach the task location.
            // Perform the task until it's complete.
            // Once complete:
            //  If resource gathering
            //      Find next resource node close to task target location.
            //          If no resource node found 
            //              Do nothing, idle at task target location.
            //          if resource node found
            //              Set task target location, task, go back to START
            //          If Building/repairing
            //              Look for more to repair/build within a small radius of task target location
            //                  If something found to build/repair
            //                      Set task target location, task, go back to START
            //                  If nothing found to build/repair
            //                      Do nothing, idle at task target location.
        }

        ResetAI();
        PlayChangeTaskAudio();
    }

    /// <summary>
    /// Changes villager type to builder and sets it's task to the passed in
    /// structure location.
    /// </summary>
    /// <param name="structure"></param>
    public override void AssignUnitToStructureTask(Structure structure)
    {
        AssignUnitTaskAndLocation(RTSUnitType.Builder, World.at(structure.GetDirectionalCoord(gridPosition)));
    }

    /// <summary>
    /// Changes villager type to builder and sets it's task to the passed in
    /// constructible location.
    /// </summary>
    /// <param name="constructible"></param>
    public override void AssignUnitToConstructibleTask(Constructible constructible)
    {
        AssignUnitTaskAndLocation(RTSUnitType.Builder, World.at(constructible.GetDirectionalCoord(gridPosition)));
    }

    /// <summary>
    /// Changes villager type to hunter and sets it's task to the passed in
    /// fauna location.
    /// </summary>
    /// <param name="fauna"></param>
    public override void AssignUnitToFaunaTask(Fauna fauna)
    {
        AssignUnitTaskAndLocation(RTSUnitType.Hunter, World.at(fauna.GetDirectionalCoord(gridPosition)));
    }

    /// <summary>
    /// Changes villager type and sets it's task to the passed in resource
    /// location.
    /// </summary>
    /// <param name="resource"></param>
    public override void AssignUnitToResourceTask(Resource resource)
    {
        switch (resource.type)
        {
            case ResourceGatheringType.Gold:
                ChangeVillagerType(RTSUnitType.GoldMiner);
                break;

            case ResourceGatheringType.Grain:
                ChangeVillagerType(RTSUnitType.Farmer);
                break;

            case ResourceGatheringType.Stone:
                ChangeVillagerType(RTSUnitType.StoneMiner);
                break;

            case ResourceGatheringType.Wood:
                ChangeVillagerType(RTSUnitType.Lumberjack);
                break;

            case ResourceGatheringType.Berries:
                ChangeVillagerType(RTSUnitType.Forager);
                break;

            case ResourceGatheringType.Fish:
                ChangeVillagerType(RTSUnitType.Fisherman);
                break;

            case ResourceGatheringType.Meat:
                ChangeVillagerType(RTSUnitType.Hunter);
                break;
        }

        AssignUnitTaskAndLocation(rtsUnitTypeData.unitType, World.at(resource.GetNearbyCoord()));
    }

    /// <summary>
    /// Sets the state, currentResource, and activates/deactivates the goals for
    /// the villager that correspond to the unitType passed in.
    /// </summary>
    /// <param name="unitType">The new type of villager to change to.</param>
    protected void ChangeVillagerType(RTSUnitType unitType)
    {
        SetUnitData(unitType);
        DeactivateAllGoals();
        goals.Get<GoalTransportResource>().active = true;

        switch (unitType)
        {
            case RTSUnitType.Builder:
                state = UnitState.BUILDANDREPAIR;
                //currentResource = ResourceGatheringType.None;
                goals.Get<GoalBuildRepair>().active = true;
                break;

            case RTSUnitType.Farmer:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Grain;
                goals.Get<GoalGatherGrain>().active = true;
                break;

            case RTSUnitType.Forager:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Berries;
                goals.Get<GoalGatherBerries>().active = true;
                break;

            case RTSUnitType.Hunter:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Meat;
                goals.Get<GoalGatherMeat>().active = true;
                goals.Get<GoalHuntFauna>().active = true;
                break;

            case RTSUnitType.Fisherman:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Fish;
                goals.Get<GoalGatherFish>().active = true;
                break;

            case RTSUnitType.Lumberjack:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Wood;
                goals.Get<GoalGatherWood>().active = true;
                break;

            case RTSUnitType.GoldMiner:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Gold;
                goals.Get<GoalGatherGold>().active = true;
                break;

            case RTSUnitType.StoneMiner:
                state = UnitState.GATHERING;
                currentResource = ResourceGatheringType.Stone;
                goals.Get<GoalGatherStone>().active = true;
                break;

            case RTSUnitType.Drifter:
                state = UnitState.ROAMING;
                //currentResource = ResourceGatheringType.None;
                ActivateAllGoals();
                break;
        }
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

        if (goal is GoalGatherFish)
        {
            fishermanHandToolDisplayObject.SetActive(true);
            currentHandToolDisplayObject = fishermanHandToolDisplayObject;
            return;
        }
        else if (goal is GoalGatherGold)
        {
            goldHandToolDisplayObject.SetActive(true);
            currentHandToolDisplayObject = goldHandToolDisplayObject;
            return;
        }
        else if (goal is GoalGatherGrain)
        {
            grainHandToolDisplayObject.SetActive(true);
            currentHandToolDisplayObject = grainHandToolDisplayObject;
            return;
        }
        else if (goal is GoalGatherStone)
        {
            stoneHandToolDisplayObject.SetActive(true);
            currentHandToolDisplayObject = stoneHandToolDisplayObject;
            return;
        }
        else if (goal is GoalGatherWood)
        {
            woodHandToolDisplayObject.SetActive(true);
            currentHandToolDisplayObject = woodHandToolDisplayObject;
            return;
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
        // else if (goal is GoalGatherBerries || goal is GoalGatherMeat)
            // Equip nothing.
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
        // if (!structure.CanDropOff(currentResource))
        //     return false;

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

        targetDamageable = fauna.AttributeHandler;
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

        // TODO: Add check for resources required for repair
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
