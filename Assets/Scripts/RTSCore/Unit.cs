using UnityEngine;
using Swordfish;
using Swordfish.Navigation;
using Valve.VR.InteractionSystem;

public enum UnitState
{
    IDLE,
    ROAMING,
    GATHERING,
    TRANSPORTING,
    BUILDANDREPAIR,
    RALLYING,
}

public class Unit : Actor, IFactioned
{    
    [SerializeField]
    protected Faction faction;
    public Faction GetFaction() { return faction; }
    public void UpdateFaction() { faction = GameMaster.Factions?.Find(x => x.Id == factionId); }

    [Header("Unit")]
    public RTSUnitType rtsUnitType;
    public GameObject rangedProjectile;
    protected GameObject projectileTarget;
    public float projectileSpeed = 5.0f;
    private GameObject projectile;
    private Vector3 projectileTargetPos;
    
    [Header("Skin Settings")]
    public MeshRenderer[] meshes;
    public SkinnedMeshRenderer[] skinnedMeshes;

    [Header("AI")]
    [SerializeField]
    protected UnitState state;
    public bool isHeld { get; protected set; }
    public bool isDying { get; protected set; }
    public bool wasThrownOrDropped { get; protected set; }
    protected UnitState previousState;

    // Make this read only, we should only be able to change unit properties
    // through the database.
    public UnitData rtsUnitTypeData 
    { 
        get 
        { 
            if (!m_rtsUnitTypeData)
                m_rtsUnitTypeData = GameMaster.GetUnit(rtsUnitType);
                
            return m_rtsUnitTypeData; 
        } 
    }

    protected UnitData m_rtsUnitTypeData;

    protected AudioSource audioSource;
    protected Animator animator;
    protected PlayerManager playerManager;
    protected float detachFromHandTime;

    public void Awake()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        if (!audioSource)
            Debug.Log("No audiosource component found.");
    }

    public override void Initialize()
    {
        base.Initialize();
        playerManager = PlayerManager.instance;

        goals.Add<GoalGotoLocation>().active = false;

        animator = gameObject.GetComponentInChildren<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");

        if (!(m_rtsUnitTypeData = GameMaster.GetUnit(rtsUnitType)))
            Debug.Log(string.Format("{0} data not found.", rtsUnitType));

        UpdateFaction();
        SetSkin();
    }

    private void SetSkin()
    {
        UpdateFaction();

        if (!faction) return;

        if (faction.skin.unitMaterial)
        {
            foreach (MeshRenderer mesh in meshes)
                mesh.sharedMaterial = faction.skin.unitMaterial;

            foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
                skinnedMesh.sharedMaterial = faction.skin.unitMaterial;
        }
    }

    //=========================================================================
    /// <summary>
    /// Sets the unit type, fetches unitData, sets maxGoalInteractRange to
    /// unitData.attackRange, and resets the goal.
    /// </summary>
    /// <param name="unitType"></param>
    public virtual void SetUnitTask(RTSUnitType unitType)
    {
        rtsUnitType = unitType;
        m_rtsUnitTypeData = GameMaster.GetUnit(rtsUnitType);
        maxGoalInteractRange = rtsUnitTypeData.attackRange;
        ResetGoal();
    }

    public virtual bool IsCivilian() { return true; }

    public bool IsDead()
    {
        return AttributeHandler.GetAttributePercent(Attributes.HEALTH) <= 0.0f;
    }

    public virtual void OnAttachedToHand(Hand hand)
    {
        isHeld = true;
        Freeze();
        wasThrownOrDropped = false;

        animator.SetInteger("AnimationActorState", (int)ActorAnimationState.IDLE);

        if(factionId == playerManager.factionId)
            audioSource.PlayOneShot(GameMaster.GetAudio("unit_pickup_friendly").GetClip(), 0.5f);
        else
            audioSource.PlayOneShot(GameMaster.GetAudio("unit_pickup_enemy").GetClip(), 0.5f);

    }

    public virtual void OnDetachedFromHand(Hand hand)
    {
        isHeld = false;
        wasThrownOrDropped = true;        
        // Unfreeze();
        detachFromHandTime = Time.time;
    }

    public virtual void OnHandHoverBegin(Hand hand)
    {
        // villagerHoverMenu.Show();
    }

    public virtual void OnHandHoverEnd(Hand hand)
    {
        // villagerHoverMenu.Hide();
    }

    // 90% damage from 6 second fall at the gravity acceleration
    // rate of 9.81.
    protected float damageMultiplier = 10.0f;// 90.0f / (9.81f * 6.0f);
    public virtual void OnCollisionEnter(Collision collision)
    {
        if (wasThrownOrDropped)
        {
            // TODO: Should do something here based on layer
            if (collision.relativeVelocity.magnitude > 4.0f)
            {
                ContactPoint contact = collision.contacts[0];
                float damage = Vector3.Dot( contact.normal, collision.relativeVelocity) * damageMultiplier;
                AttributeHandler.Damage(damage, AttributeChangeCause.NATURAL, null, DamageType.BLUDGEONING);

                audioSource.PlayOneShot(GameMaster.GetAudio("unit_damaged").GetClip(), 0.25f);
                transform.rotation = Quaternion.identity;
                wasThrownOrDropped = false;

                Unfreeze();                
                // Debug.Log(string.Format("Magnitude: {0} Damage: {1} Health: {2}", collision.relativeVelocity.magnitude,
                //             damage, AttributeHandler.GetAttributePercent(Attributes.HEALTH).ToString()));
            }
        }
    }

    /// <summary>
    /// Brute force a unit to go to a position on the map optionally disabling all
    /// active goals.
    /// </summary>
    /// <param name="position">Position to go to in transform space.</param>
    /// <param name="deactivateGoals">Should all goals be deactivated? True/False</param>
    public virtual void MoveToPosition(Vector3 position, bool deactivateGoals = false)
    {        
        if (deactivateGoals)
            DeactivateAllGoals();
        Coord2D pos = World.ToWorldCoord(position);
        ResetPath();    
        GotoForced(pos.x, pos.y);
        LockPath();
    }

    /// <summary>
    /// Use GoalGotoLocation to send a unit to a position on the map optionally disabling all
    /// active goals.
    /// </summary>
    /// <param name="position">Target position to go to in transform space.</param>
    /// <param name="deactivateGoals">Should all goals be deactivated? True / False</param>
    public virtual void MoveToLocation(Vector3 position, bool deactivateGoals = false)
    {
        if (deactivateGoals)
            DeactivateAllGoals();

        Coord2D pos = World.ToWorldCoord(position);

        GoalGotoLocation goal = goals.Get<GoalGotoLocation>();
        if (goal != null)
        {
            goal.active = true;
            TrySetGoal(World.at(pos));
        }
        else
            Debug.Log("GoalGotoLocation not found.");
    }

    public void ActivateAllGoals()
    {
        foreach (PathfindingGoal goal in GetGoals())
        {
            goal.active = true;
        }
    }

    public void DeactivateAllGoals()
    {
        foreach(PathfindingGoal goal in GetGoals())
        {
            goal.active = false;
        }
    }

    public virtual void Update()
    {
        if (projectile)
            LaunchProjectile();
    }

    public virtual void LaunchProjectile()
    {
        if (!projectile)
        {
            projectile = Instantiate(rangedProjectile);
            projectile.transform.position = transform.position;
            projectile.transform.position += new Vector3(0, 0.09f, 0);
            projectileTargetPos = projectileTarget.transform.position;
            projectileTargetPos += new Vector3(0, 0.09f, 0);

            if (projectileTarget)
            {
                projectileTargetPos = projectileTarget.transform.position;
                projectileTargetPos += new Vector3(0, 0.09f, 0);
            }
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
            projectile.transform.Translate(Vector3.forward * (projectileSpeed * Time.deltaTime));
        }
    }
    void OnValidate()
    {
        if (!GameMaster.Instance)
            return;

        UpdateFaction();
        SetSkin();
    }
}
