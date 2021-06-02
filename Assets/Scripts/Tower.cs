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


    protected Unit currentTarget;
    protected float attackTimer;
    private SphereCollider attackRangeCollider;
    protected byte targetCount;
    public override void Initialize()
    {
        base.Initialize();
        attackRangeCollider = GetComponent<SphereCollider>();
        attackRangeCollider.radius = attackRange * World.GetUnit();
    }

    void Update()
    {
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
        
        currentTarget?.AttributeHandler.Damage(attackDamage, Swordfish.AttributeChangeCause.ATTACKED, AttributeHandler, Swordfish.DamageType.PIERCING);
        
            
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

}
