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

    [Header("Soldier")]
    protected Animator animator;
    private AudioSource audioSource;

    [Header ("Visuals")]
    //public VillagerHoverMenu villagerHoverMenu;

    bool isHeld;   
    bool isDead;

    public void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (!audioSource)
            Debug.Log("No audiosource component found.");
    }    

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
        
        AttributeHandler.OnDamageEvent += OnDamage;
    }

    public void HookIntoEvents()
    {
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;
    }

    public void SetAIAttackGoals(bool villagers, bool military, bool buildings)
    {
        if (villagers)
            goals.Add<GoalHuntVillagers>().myFactionID = factionID;

        if (military)
            goals.Add<GoalHuntMilitary>().myFactionID = factionID;        
        
        if (buildings)
            goals.Add<GoalHuntBuildings>().myFactionID = factionID;
    }

    bool StateChanged() { return state != previousState; }

    public void OnHandHoverBegin(Hand hand)
    {
        // villagerHoverMenu.Show();
    }

    public void OnHandHoverEnd(Hand hand)
    {
        // villagerHoverMenu.Hide();
    }

    public void OnAttachedToHand(Hand hand)
    {
        isHeld = true;
        //villagerHoverMenu.Show();
        Freeze();
        animator.SetInteger("VillagerActorState", (int)ActorAnimationState.IDLE);
        audioSource.PlayOneShot(GameMaster.GetAudio("unitPickup").GetClip(), 0.5f);
    }

    public void OnDetachedFromHand(Hand hand)
    {
        isHeld = false;
        //villagerHoverMenu.Hide();
        ResetAI();
        Unfreeze();
    }

    public override void Tick()
    {
        if (isHeld || isDead)
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

    

    void OnDamage(object sender, Damageable.DamageEvent e)
    {
        if (AttributeHandler.GetAttributePercent(Attributes.HEALTH) <= 0.0f)
        {
            isDead = true;
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.DYING);
            Destroy(this.gameObject, 3.0f);
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

        // Debug.Log(string.Format("Found target {0}!", e.cell.GetFirstOccupant().name));

        //  default cancel the goal so that another can take priority
        //ResetGoal();
        //e.Cancel();
    }

    public void OnGoalInteract(object sender, PathfindingGoal.GoalInteractEvent e)
    {
        if (e.actor != this) 
            return;


        if (e.goal is GoalHuntUnits || e.goal is GoalHuntMilitary || e.goal is GoalHuntVillagers)
        {
            Unit unit = e.cell.GetFirstOccupant<Unit>();            
            Damageable damageable = unit.GetComponent<Damageable>();
            damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, null, DamageType.SLASHING);
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.ATTACKING);
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
            animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.ATTACKING);
            return;
        }
        else if (e.goal is GoalSearchAndDestroy)
        {
            Unit unit = e.cell.GetFirstOccupant<Unit>();
            if (unit)
            {
                Damageable damageable = unit.GetComponent<Damageable>();
                damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, null, DamageType.SLASHING);
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.ATTACKING);
                return;
            }

            Structure structure = e.cell.GetFirstOccupant<Structure>();
            if (structure)
            {
                Damageable damageable = structure.GetComponent<Damageable>();
                damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, null, DamageType.SLASHING);
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.ATTACKING);
                return;
            }

            Constructible construction = e.cell.GetFirstOccupant<Constructible>();
            if (construction)
            {
                Damageable damageable = construction.GetComponent<Damageable>();
                damageable.Damage(rtsUnitTypeData.attackDamage, AttributeChangeCause.ATTACKED, null, DamageType.SLASHING);
                animator.SetInteger("ActorAnimationState", (int)ActorAnimationState.ATTACKING);
                return;
            }
        }
        
        //  default cancel the interaction
        ResetGoal();
        e.Cancel();
    }   

    public void CleanupEvents()
    {
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;
    }

    public void OnDestroy()
    {
        CleanupEvents();
    }
}
