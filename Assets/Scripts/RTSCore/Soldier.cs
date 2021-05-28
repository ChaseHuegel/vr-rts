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

    [Header("Unit")]
    public GameObject rangedProjectile;
    public bool isRanged;
    //public VillagerHoverMenu villagerHoverMenu;

    protected GameObject projectileTarget;
    protected float arrowSpeed = 5.0f;

    public override void Initialize()
    {
        base.Initialize();
        HookIntoEvents();        
        
        maxGoalInteractRange = rtsUnitTypeData.attackRange;

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
        PathfindingGoal.OnGoalChangeEvent += OnGoalChange;
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

        //if (isRanged)
        // if (DistanceTo(e.cell) < 10)
        //      Debug.Log(string.Format("Found target {0}!", e.cell.GetFirstOccupant().name));

        //  default cancel the goal so that another can take priority
        //ResetGoal();
        //e.Cancel();
    }

    public void OnGoalChange(object sender, PathfindingGoal.GoalChangeEvent e)
    {
        if (e.actor != this)
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
                damageable = structure.GetComponent<Damageable>();
            else
                damageable = e.cell.GetFirstOccupant<Constructible>().GetComponent<Damageable>();
            
            projectileTarget = structure.gameObject;

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

    void Update()
    {
        if (projectile)
            LaunchProjectile();

    }

    GameObject projectile;
    Vector3 projectileTargetPos;
    public void LaunchProjectile()
    {
        if (!projectile)
        {
            projectile = Instantiate(rangedProjectile);
            projectile.transform.position = transform.position;
            projectile.transform.position += new Vector3(0, 0.09f, 0);            
            projectileTargetPos = projectileTarget.transform.position;
            projectileTargetPos += new Vector3(0, 0.09f, 0);    
        }

        // First we get the direction of the arrow's forward vector to the target position.
        Vector3 tDir = projectileTargetPos - projectile.transform.position;
        

        // Now we use a Quaternion function to get the rotation based on the direction
        Quaternion rot = Quaternion.LookRotation(tDir);
    
        // And finally, set the arrow's rotation to the one we just created.
        projectile.transform.rotation = rot;
    
        //Get the distance from the arrow to the target
        float dist = Vector3.Distance(projectile.transform.position, projectileTargetPos);
    
        if(dist <= 0.1f)
        {
            // This will destroy the arrow when it is within .1 units
            // of the target location. You can set this to whatever
            // distance you're comfortable with.
            GameObject.Destroy(projectile);
    
        }
        else
        {
            // If not, then we just keep moving forward
            projectile.transform.Translate(Vector3.forward * (arrowSpeed * Time.deltaTime));
        }
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
