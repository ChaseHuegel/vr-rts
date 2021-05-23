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
        
        SetAIAttackGoals(huntVillagers, huntMilitary, huntBuildings);

        animator = gameObject.GetComponentInChildren<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");

        if(PlayerManager.instance.factionID == factionID)
            PlayerManager.instance.AddToPopulation((Unit)this);     

    }
    
    public void HookIntoEvents()
    {
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;
        AttributeHandler.OnDamageEvent += OnDamage;
        Damageable.OnDeathEvent += OnDeath;
    }

    public void SetAIAttackGoals(bool villagers, bool military, bool buildings)
    {
        if (villagers)
            goals.Add<GoalHuntVillagers>().myFactionID = factionID;

        if (military)
            goals.Add<GoalHuntMilitary>().myFactionID = factionID;        
        
        if (buildings)
            goals.Add<GoalHuntBuildings>().myFactionID = factionID;

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

     // Used by animator to play sound effects
    public void AnimatorPlayAudio(string clipName)
    {
        AudioSource.PlayClipAtPoint(GameMaster.GetAudio(clipName).GetClip(), transform.position, 0.75f);
    }

    public void OnGoalFound(object sender, PathfindingGoal.GoalFoundEvent e)
    {
        if (e.actor != this) 
            return;

        //Debug.Log(string.Format("Found target {0}!", e.cell.GetFirstOccupant().name));

        //  default cancel the goal so that another can take priority
        //ResetGoal();
        //e.Cancel();
    }

    public void OnGoalInteract(object sender, PathfindingGoal.GoalInteractEvent e)
    {
        if (e.actor != this || isHeld) 
            return;
        
        if (e.goal is GoalHuntUnits || e.goal is GoalHuntMilitary || e.goal is GoalHuntVillagers)
        {
            Unit unit = e.cell.GetFirstOccupant<Unit>();
            

            Damageable damageable = unit.GetComponent<Damageable>();            
            damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, null, DamageType.SLASHING);
            
            if (unit.IsDead())
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);
            else
                SetAttackAnimationState();

            return;
        }
        else if (e.goal is GoalHuntBuildings)
        {                     
            Damageable damageable; 
            Structure structure = e.cell.GetFirstOccupant<Structure>();  
            if (structure) 
                damageable = structure.GetComponent<Damageable>();
            else
                damageable = e.cell.GetFirstOccupant<Constructible>().GetComponent<Damageable>();
            
            damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, null, DamageType.SLASHING);
            SetAttackAnimationState();
            return;
        }
        else if (e.goal is GoalSearchAndDestroy)
        {
            Unit unit = e.cell.GetFirstOccupant<Unit>();
            if (unit)
            {
                Damageable damageable = unit.GetComponent<Damageable>();
                damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, null, DamageType.SLASHING);
               
                if (unit.IsDead())
                    animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.IDLE);
                else
                    SetAttackAnimationState();
                    
                return;
            }

            Structure structure = e.cell.GetFirstOccupant<Structure>();
            if (structure)
            {
                Damageable damageable = structure.GetComponent<Damageable>();
                damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, null, DamageType.SLASHING);
                SetAttackAnimationState();
                return;
            }

            Constructible construction = e.cell.GetFirstOccupant<Constructible>();
            if (construction)
            {
                Damageable damageable = construction.GetComponent<Damageable>();
                damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, null, DamageType.SLASHING);
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
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;
        Damageable.OnDeathEvent -= OnDeath;
    }

    public void OnDestroy()
    {
        CleanupEvents();
    }
}
