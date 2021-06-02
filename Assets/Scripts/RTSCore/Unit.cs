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
    public void UpdateFaction() { faction = GameMaster.Factions.Find(x => x.index == factionId); }

    [Header("Unit")]
    public RTSUnitType rtsUnitType;
     public GameObject rangedProjectile;
    protected GameObject projectileTarget;
    protected float arrowSpeed = 5.0f;
    GameObject projectile;
    Vector3 projectileTargetPos;

    [Header("AI")]
    public UnitState state;

    public bool isHeld;
    public bool isDying;
    public bool wasThrownOrDropped;
    protected UnitState previousState;

    // Make this read only, we should only be able to change unit properties
    // through the database.
    public UnitData rtsUnitTypeData { get { return m_rtsUnitTypeData; } }
    protected UnitData m_rtsUnitTypeData;

    public AudioSource audioSource;
    protected Animator animator;
    protected PlayerManager playerManager;

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

        animator = gameObject.GetComponentInChildren<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");

        if (!m_rtsUnitTypeData)
            m_rtsUnitTypeData = GameMaster.GetUnit(rtsUnitType);

        UpdateFaction();
    }

    //=========================================================================
    // Sets the unit type and unitTypeData
    public virtual void SetUnitType(RTSUnitType unitType)
    {
        rtsUnitType = unitType;
        m_rtsUnitTypeData = GameMaster.GetUnit(rtsUnitType);
        maxGoalInteractRange = rtsUnitTypeData.attackRange;
        ResetGoal();
    }

    public virtual bool IsCivilian()
    {
        return (int)rtsUnitTypeData.unitType < (int)RTSUnitType.Swordsman;
    }

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
        ResetAI();
        Unfreeze();
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

                // Debug.Log(string.Format("Magnitude: {0} Damage: {1} Health: {2}", collision.relativeVelocity.magnitude,
                //             damage, AttributeHandler.GetAttributePercent(Attributes.HEALTH).ToString()));
            }
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
}
