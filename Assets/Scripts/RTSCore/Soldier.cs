using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Damageable))]
public class Soldier : Unit
{
    [Header("AI")]
    public bool huntVillagers = true;
    public bool huntMilitary = true;
    public bool huntBuildings = true;

    //public VillagerHoverMenu villagerHoverMenu;

    public override void Initialize()
    {
        base.Initialize();
        HookIntoEvents();

        SetAIAttackGoals(huntMilitary, huntVillagers, huntBuildings);

        animator = gameObject.GetComponentInChildren<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");

        // if(faction.IsSameFaction(playerManager.factionId))
        //     playerManager.AddToPopulation((Unit)this);

    }

    public void HookIntoEvents()
    {
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;
        PathfindingGoal.OnGoalChangeEvent += OnGoalChange;
        AttributeHandler.OnDamageEvent += OnDamaged;
        Damageable.OnDeathEvent += OnDeath;
    }


    public void CleanupEvents()
    {
        PathfindingGoal.OnGoalChangeEvent -= OnGoalChange;
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;
        AttributeHandler.OnDamageEvent -= OnDamaged;
        Damageable.OnDeathEvent -= OnDeath;
    }

    /// <summary>
    /// Sets the soldiers task to the passed in structure location. If structure
    /// is an enemy structure soldier is set attacks the
    /// structure. If structure is friendly soldier moves to location.
    /// </summary>
    /// <param name="structure"></param>
    public override void AssignUnitToStructureTask(Structure structure)
    {
        if (IsSameFaction(structure.factionId))
            MoveToLocation(structure.transform.position);
        else
        {
            goals.Get<GoalHuntBuildings>().active = true;
            TrySetGoal(World.at(structure.GetNearbyCoord()));
        }
    }

    /// <summary>
    /// Sets the soldiers task to the passed in constructible location. If
    /// constructible is an enemy constructible soldier is set attacks the
    /// constructible. If constructible is friendly soldier moves to location.
    /// </summary>
    /// <param name="constructible"></param>
    public override void AssignUnitToConstructibleTask(Constructible constructible)
    {
        if (IsSameFaction(constructible.factionId))
            MoveToLocation(constructible.transform.position);
        else
        {
            goals.Get<GoalHuntBuildings>().active = true;
            TrySetGoal(World.at(constructible.GetNearbyCoord()));
        }
    }

    /// <summary>
    /// Sets the soldiers task to the passed in unit location.
    /// </summary>
    /// <param name="unit"></param>
    public override void AssignUnitToUnitTask(Unit unit)
    {
        if (IsSameFaction(unit))
            MoveToLocation(unit.transform.position);
        else
        {
            if (unit.IsCivilian())
                goals.Get<GoalHuntVillagers>().active = true;
            else
                goals.Get<GoalHuntMilitary>().active = true;

            TrySetGoal(unit.GetCellAtGrid());
        }
    }

    /// <summary>
    /// Sets the soldiers task to the passed in fauna and sets
    /// the soldier to attack it.
    /// </summary>
    /// <param name="fauna"></param>
    public override void AssignUnitToFaunaTask(Fauna fauna)
    {
        TrySetGoal(fauna.GetCellAtGrid());
    }

    public void SetAIAttackGoals(bool military, bool villagers,  bool buildings)
    {
        goals.Add<GoalHuntMilitary>().active = false;
        goals.Add<GoalHuntVillagers>().active = false;
        goals.Add<GoalHuntBuildings>().active = false;

        if (military)
            goals.Get<GoalHuntMilitary>().active = true;

        if (villagers)
            goals.Get<GoalHuntVillagers>().active = true;

        if (buildings)
            goals.Get<GoalHuntBuildings>().active = true;

        //ResetAI();
    }

    bool StateChanged() { return state != previousState; }

    public override void OnHandHoverBegin(Hand hand)
    {
        base.OnHandHoverBegin(hand);
        // villagerHoverMenu.Show();
    }

    public override void OnHandHoverEnd(Hand hand)
    {
        base.OnHandHoverEnd(hand);
        // villagerHoverMenu.Hide();
    }

    public override void OnAttachedToHand(Hand hand)
    {
        base.OnAttachedToHand(hand);
    }

    public override void OnDetachedFromHand(Hand hand)
    {
        base.OnDetachedFromHand(hand);
    }

    public override void Tick()
    {
        if (isHeld || isDying)
            return;

        base.Tick();

        GotoNearestGoalWithPriority();

        if (IsMoving() )
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.MOVING);
        else if (IsIdle())
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);

        // if (TaskChanged())
        // {
        //     ChangeEquippedItems();
        //     //PlayChangeTaskAudio();
        // }

        previousState = state;
    }

    public void OnDamaged(object sender, Damageable.DamageEvent e)
    {
        if (e.victim != AttributeHandler)
            return;

        if (!currentGoalCell.GetOccupant<Soldier>())
            TrySetGoal(e.attacker.GetComponentInChildren<Body>().GetCellAtGrid());
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

    public override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    // This is is used to reenable the character after they have been
    // released from the hand AND after they have landed somewhere.
    void OnTriggerEnter(Collider collider)
    {
        if (!wasThrownOrDropped)
            return;

        // TODO: could just switch this to a cell lookup where they land.
        // Don't wait for a collision indefinitely.
        if (Time.time - detachFromHandTime >= 2.0f)
        {
            wasThrownOrDropped = false;
            return;
        }

        Unfreeze();

        Unit unit = collider.gameObject.GetComponent<Unit>();
        if (unit)
        {
            AssignUnitToUnitTask(unit);
            return;
        }

        // Soldiers should kill fauna they are tasked to target.
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

     // Used by animator to play sound effects
    // public void AnimatorPlayAudio(string clipName)
    // {
    //     AudioSource.PlayClipAtPoint(GameMaster.GetAudio(clipName).GetClip(), transform.position, 0.75f);
    // }


    public void OnGoalFound(object sender, PathfindingGoal.GoalFoundEvent e)
    {
        if (e.actor != this)
            return;

        //  default cancel the goal so that another can take priority
        //ResetGoal();
        //e.Cancel();
    }

    public void OnGoalChange(object sender, PathfindingGoal.GoalChangeEvent e)
    {
        if (e.actor != this || isDying)
            return;

        targetDamageable = null;

        if (previousGoal is GoalHuntUnits || previousGoal is GoalHuntMilitary || previousGoal is GoalHuntVillagers)
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);
    }

    public override void Strike(string audioClipName = "")
    {
        base.Strike(audioClipName);

        if (targetDamageable)
            targetDamageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, AttributeHandler, DamageType.SLASHING);
    }

    public void OnGoalInteract(object sender, PathfindingGoal.GoalInteractEvent e)
    {
        if (e.actor != this || isHeld)
            return;

        //targetDamageable = null;

        if (e.goal is GoalGotoLocation)
        {
            ActivateAllGoals();
            e.goal.active = false;
            targetDamageable = null;
            return;
        }

        Unit unit = e.cell.GetFirstOccupant<Unit>();
        if (unit)
        {
            targetDamageable = unit.AttributeHandler;
            SetAttackAnimationState();
            return;
        }

        Structure structure = e.cell.GetFirstOccupant<Structure>();
        if (structure)
        {
            targetDamageable = structure.AttributeHandler;
            SetAttackAnimationState();
            return;
        }

        Constructible constructible = e.cell.GetFirstOccupant<Constructible>();
        if (constructible)
        {
            targetDamageable = constructible.AttributeHandler;
            SetAttackAnimationState();
            return;
        }

        // // if (e.goal is GoalHuntUnits || e.goal is GoalHuntMilitary || e.goal is GoalHuntVillagers)
        // // {
        // //     Unit unit = e.cell.GetFirstOccupant<Unit>();
        // //     targetDamageable = unit.AttributeHandler;

        // //     // Damageable damageable = unit.GetComponent<Damageable>();
        // //     // damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, AttributeHandler, DamageType.SLASHING);
        // //     SetAttackAnimationState();
        // //     return;
        // // }
        // // else if (e.goal is GoalHuntBuildings)
        // // {
        // //     Structure structure = e.cell.GetFirstOccupant<Structure>();
        // //     if (structure)
        // //         targetDamageable = structure.AttributeHandler;
        // //     else
        // //         targetDamageable = e.cell.GetFirstOccupant<Constructible>().AttributeHandler;

        // //     return;
        // // }
        // // else if (e.goal is GoalSearchAndDestroy)
        // // {
        // //     Unit unit = e.cell.GetFirstOccupant<Unit>();
        // //     if (unit)
        // //     {
        // //         targetDamageable = unit.AttributeHandler;
        // //         return;
        // //     }

        // //     Structure structure = e.cell.GetFirstOccupant<Structure>();
        // //     if (structure)
        // //     {
        // //         targetDamageable = structure.AttributeHandler;
        // //         return;
        // //     }

        // //     Constructible construction = e.cell.GetFirstOccupant<Constructible>();
        // //     if (construction)
        // //     {
        // //         targetDamageable = construction.AttributeHandler;
        // //         return;
        // //     }
        // // }
        // // else if (e.goal is GoalGotoLocation)
        // // {
        // //     ActivateAllGoals();
        // //     e.goal.active = false;
        // // }

        //  default cancel the interaction
        ResetGoal();
        e.Cancel();
    }

    public override bool IsCivilian() { return false; }

    private void SetAttackAnimationState()
    {
        if (UnityEngine.Random.Range(1, 100) < 50)
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.ATTACKING);
        else
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.ATTACKING2);
    }


    public void OnDestroy()
    {
        CleanupEvents();
    }
}
