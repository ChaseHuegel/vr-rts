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
    public UnitState state;
    protected UnitState previousState;   

    [Header("Soldier")]
    protected Animator animator;
    private AudioSource audioSource;

    [Header ("Visuals")]
    //public VillagerHoverMenu villagerHoverMenu;

    bool isHeld;

    public void HookIntoEvents()
    {
        PathfindingGoal.OnGoalFoundEvent += OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent += OnGoalInteract;
    }

    public void CleanupEvents()
    {
        PathfindingGoal.OnGoalFoundEvent -= OnGoalFound;
        PathfindingGoal.OnGoalInteractEvent -= OnGoalInteract;
    }

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

        SetUnitType(rtsUnitType);

        animator = gameObject.GetComponentInChildren<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");

        PlayerManager.instance.AddToPopulation((Unit)this);
    }

    public void OnDestroy()
    {
        CleanupEvents();
    }

    bool StateChanged()
    {
        return state != previousState;
    }

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
        animator.SetInteger("VillagerActorState", 0);
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
        if (isHeld)
            return;

        base.Tick();

        //GotoNearestGoalWithPriority();
        
        //Debug.Log("State: " + state.ToString() + " PreviousState: " + previousState.ToString());

        if (IsMoving() )
        {
            animator.SetInteger("VillagerActorState", (int)ActorAnimationState.MOVING);
        }
        
        // if (TaskChanged())
        // {
        //     ChangeEquippedItems();
        //     //PlayChangeTaskAudio();
        // }

        previousState = state;
    }

     // Used by animator to play sound effects
    public void AnimatorPlayAudio(string clipName)
    {
        AudioSource.PlayClipAtPoint(GameMaster.GetAudio(clipName).GetClip(), transform.position, 0.75f);
    }

    public void OnGoalFound(object sender, PathfindingGoal.GoalFoundEvent e)
    {
        if (e.actor != this) return;

        // Soldier villager = (Soldier)e.actor;

        // //  Need C# 7 in Unity for switching by type!!!
        // if (e.goal is GoalGatherResource && !villager.IsCargoFull())
        // {
        //     return;
        // }

        //  default cancel the goal so that another can take priority
        //ResetGoal();
        //e.Cancel();
    }

    public void OnGoalInteract(object sender, PathfindingGoal.GoalInteractEvent e)
    {
        if (e.actor != this) return;

        // Villager villager = (Villager)e.actor;
        // Resource resource = e.cell.GetOccupant<Resource>();
        // Structure structure = e.cell.GetOccupant<Structure>();
        // Constructible construction = e.cell.GetOccupant<Constructible>();
        
        // if  (e.goal is GoalGatherResource && villager.TryGather(resource) ||
        //     (e.goal is GoalTransportResource && villager.TryDropoff(structure) ||
        //     (e.goal is GoalBuildRepair && (villager.TryRepair(structure) || villager.TryBuild(construction))))) 
        // {
        //     return;
        // } 
        
        // //  default cancel the interaction
        // ResetGoal();
        // e.Cancel();
    }   
}
