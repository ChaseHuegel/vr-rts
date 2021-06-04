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

        maxGoalInteractRange = rtsUnitTypeData.attackRange;

        SetAIAttackGoals(huntVillagers, huntMilitary, huntBuildings);

        animator = gameObject.GetComponentInChildren<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");

        if(faction.IsSameFaction(playerManager.factionId))
            playerManager.AddToPopulation((Unit)this);

    }

    public void HookIntoEvents()
    {
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;
        PathfindingGoal.OnGoalChangeEvent += OnGoalChange;
        AttributeHandler.OnDamageEvent += OnDamage;
        Damageable.OnDeathEvent += OnDeath;
    }

    public void SetAIAttackGoals(bool villagers, bool military, bool buildings)
    {
        if (villagers)
            goals.Add<GoalHuntVillagers>();

        if (military)
            goals.Add<GoalHuntMilitary>();

        if (buildings)
            goals.Add<GoalHuntBuildings>();

        ResetAI();
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

    public void OnDamage(object sender, Damageable.DamageEvent e)
    {
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

        // TODO: could just switch this to a cell lookup where
        // TODO: they land.
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
            if (!IsSameFaction(unit))
                TrySetGoal(unit.GetCellAtGrid());

            return;
        }

        // Soldiers should kill fauna they are tasked to target.
        Fauna fauna = collider.gameObject.GetComponent<Fauna>();
        if (fauna)
        {
            return;
        }

        Structure building = collider.gameObject.GetComponentInParent<Structure>();
        if (building)
        {
            if (!IsSameFaction(building.factionId))
                TrySetGoal(building.GetCellAtGrid());

            return;
        }

        Constructible constructible = collider.gameObject.GetComponentInParent<Constructible>();
        if (constructible)
        {
            if (!IsSameFaction(constructible.factionId))
                TrySetGoal(constructible.GetCellAtGrid());
            
            return;
        }
    }

     // Used by animator to play sound effects
    public void AnimatorPlayAudio(string clipName)
    {
        AudioSource.PlayClipAtPoint(GameMaster.GetAudio(clipName).GetClip(), transform.position, 0.75f);
    }


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

        if (previousGoal is GoalHuntUnits || previousGoal is GoalHuntMilitary || previousGoal is GoalHuntVillagers)
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);
    }

    public void OnGoalInteract(object sender, PathfindingGoal.GoalInteractEvent e)
    {
        if (e.actor != this || isHeld)
            return;

        if (e.goal is GoalHuntUnits || e.goal is GoalHuntMilitary || e.goal is GoalHuntVillagers)
        {
            Unit unit = e.cell.GetFirstOccupant<Unit>();
            projectileTarget = unit.gameObject;
            Damageable damageable = unit.GetComponent<Damageable>();
            damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, AttributeHandler, DamageType.SLASHING);
            SetAttackAnimationState();
            return;
        }
        else if (e.goal is GoalHuntBuildings)
        {
            Damageable damageable;
            Structure structure = e.cell.GetFirstOccupant<Structure>();
            if (structure)
            {
                damageable = structure.GetComponent<Damageable>();
                projectileTarget = structure.gameObject;
            }
            else
                damageable = e.cell.GetFirstOccupant<Constructible>().GetComponent<Damageable>();

            if (!damageable)
                return;
                
            damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, AttributeHandler, DamageType.SLASHING);
            SetAttackAnimationState();
            return;
        }
        else if (e.goal is GoalSearchAndDestroy)
        {
            Unit unit = e.cell.GetFirstOccupant<Unit>();
            if (unit)
            {
                projectileTarget = unit.gameObject;
                Damageable damageable = unit.GetComponent<Damageable>();
                damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, AttributeHandler, DamageType.SLASHING);
                SetAttackAnimationState();
                return;
            }

            Structure structure = e.cell.GetFirstOccupant<Structure>();
            if (structure)
            {
                projectileTarget = structure.gameObject;
                Damageable damageable = structure.GetComponent<Damageable>();
                damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, AttributeHandler, DamageType.SLASHING);
                SetAttackAnimationState();
                return;
            }

            Constructible construction = e.cell.GetFirstOccupant<Constructible>();
            if (construction)
            {
                projectileTarget = construction.gameObject;
                Damageable damageable = construction.GetComponent<Damageable>();
                damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, AttributeHandler, DamageType.SLASHING);
                SetAttackAnimationState();
                return;
            }
        }

        //  default cancel the interaction
        ResetGoal();
        e.Cancel();
    }

    private void SetAttackAnimationState()
    {
        if (UnityEngine.Random.Range(1, 100) < 50)
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.ATTACKING);
        else
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.ATTACKING2);
    }

    public void CleanupEvents()
    {
        PathfindingGoal.OnGoalChangeEvent -= OnGoalChange;
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;
        Damageable.OnDeathEvent -= OnDeath;
    }

    public void OnDestroy()
    {
        CleanupEvents();
    }
}
