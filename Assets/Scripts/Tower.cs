using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;

[RequireComponent(typeof(SphereCollider))]
public class Tower : Structure
{
    [Tooltip("Radius in grid cells of the tower's attack.")]
    public float attackRange = 5;


    [Tooltip("Time between attacks.")]
    public float attackSpeed = 3.0f;
    public float attackDamage = 1.0f;
    public GameObject rangedProjectile;
    public float projectileSpeed = 5.0f;
    public Transform projectileOrigin;
    protected Unit currentTarget;
    protected float attackTimer;
    private SphereCollider attackRangeCollider;    
    protected GameObject projectileTarget;    
    private GameObject projectile;
    private Vector3 projectileTargetPos;

    public override void Initialize()
    {
        base.Initialize();
        attackRangeCollider = GetComponent<SphereCollider>();
        attackRangeCollider.radius = attackRange * World.GetUnit();

        if (!projectileOrigin)
            projectileOrigin = transform;
    }

    void Update()
    {
        if (projectile)
            LaunchProjectile();

        if (!currentTarget)// || targetCount <= 0)
           return;

        if (attackTimer >= attackSpeed)
        {
            Attack();
            attackTimer = 0.0f;
        }

        attackTimer += Time.deltaTime;

    }

    void Attack()
    {
        if (!currentTarget || currentTarget.IsDead())
            TryAcquireNewTarget();

        if (currentTarget)
        {
            currentTarget.AttributeHandler.Damage(attackDamage, Swordfish.AttributeChangeCause.ATTACKED, AttributeHandler, Swordfish.DamageType.PIERCING);
            projectileTarget = currentTarget.gameObject;
            LaunchProjectile();
        }


    }

    void TryAcquireNewTarget()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, attackRangeCollider.radius);
      
        foreach(Collider collider in targets)
        {
            Unit unit = collider.GetComponentInParent<Unit>();
            if (unit && !unit.IsSameFaction(factionId) && !unit.IsDead())
            {
                currentTarget = unit;
                break;
            }
        }

        if (currentTarget && currentTarget.IsDead())
            currentTarget = null;

    }

    void OnTriggerEnter(Collider other)
    {
        Unit unit = other.GetComponent<Unit>();
        if (unit)
        {
            if (!unit.IsSameFaction(factionId))
            {
                if (!currentTarget || currentTarget.IsDead())
                    currentTarget = unit;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        Unit unit = other.GetComponent<Unit>();
        if (unit)
        {
            if (!unit.IsSameFaction(factionId))
            {
                if (currentTarget == unit || currentTarget.IsDead())
                    currentTarget = null;                    

                if (currentTarget == null)
                    TryAcquireNewTarget();
            }
        }
    }

    public virtual void LaunchProjectile()
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
}
