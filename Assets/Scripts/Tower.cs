using System.Collections;
using System.Collections.Generic;
using Swordfish.Navigation;
using UnityEngine;

public class Tower : Structure
{
    [Tooltip("Building attack radius in grid units/cells.")]
    public int attackRange = 5;
    [Tooltip("Seconds between attacks.")]
    public float attackSpeed = 3.0f;
    public float attackDamage = 1.0f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform projectileOrigin;    
    protected Body currentTarget;
    protected float timeSinceLastAttack;
    private SphereCollider attackRangeCollider;
    protected GameObject projectileTarget;
    

    public override void Initialize()
    {
        base.Initialize();
        attackRangeCollider = GetComponentInChildren<SphereCollider>();
        attackRangeCollider.radius = attackRange * World.GetUnit();

        if (!projectileOrigin)
            projectileOrigin = transform;
    }

    void Update()
    {
        if (!currentTarget)// || targetCount <= 0)
            return;

        if (timeSinceLastAttack >= attackSpeed)
        {
            Attack();
            timeSinceLastAttack = 0.0f;
        }

        timeSinceLastAttack += Time.deltaTime;

    }

    void Attack()
    {
        if (!currentTarget || !currentTarget.IsAlive())
            TryAcquireNewTarget();

        if (currentTarget)
        {
            projectileTarget = currentTarget.gameObject;
            TrySpawnProjectile();
        }
    }

    void TryAcquireNewTarget()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRangeCollider.radius);

        foreach (Collider collider in targets)
        {
            UnitV2 unit = collider.GetComponentInParent<UnitV2>();
            if (unit && !unit.Faction.IsSameFaction(Faction) && unit.IsAlive())
            {
                currentTarget = unit;
                break;
            }
        }

        if (currentTarget && !currentTarget.IsAlive())
            currentTarget = null;

    }

    void OnTriggerEnter(Collider other)
    {
        UnitV2 unit = other.GetComponent<UnitV2>();
        if (unit)
        {
            if (!unit.Faction.IsSameFaction(Faction))
            {
                if (!currentTarget || !currentTarget.IsAlive())
                    currentTarget = unit;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        UnitV2 unit = other.GetComponent<UnitV2>();
        if (unit)
        {
            if (!unit.Faction.IsSameFaction(Faction))
            {
                if (currentTarget == unit || !currentTarget.IsAlive())
                    currentTarget = null;

                if (currentTarget == null)
                    TryAcquireNewTarget();
            }
        }
    }

    public virtual void TrySpawnProjectile()
    {
        if (projectilePrefab && projectileTarget)
            Projectile.Spawn(projectilePrefab, projectileOrigin.position, Quaternion.identity, this, projectileTarget.transform);
    }


    #region OldCode
    /* 
    private GameObject projectile;
    private Vector3 projectileTargetPos;
    public GameObject rangedProjectile;
    public float projectileSpeed = 5.0f;    

    public virtual void TryLaunchProjectile()
    {
        if (!projectile)
        {
            projectile = Instantiate(rangedProjectile);
            projectile.transform.position = projectileOrigin.position;
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

        if (dist <= 0.1f)
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
    */
    #endregion
}
