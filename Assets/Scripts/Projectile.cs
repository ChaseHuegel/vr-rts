using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public DamageType damageType;
    public float damage = 1.0f;
    public GameObject hitFx;

    [Tooltip("Speed of projectile.")]
    public float speed = 1.0f; // Speed of projectile.

    [Tooltip("Damage radius in cells around the target point. 0 is target only.")]
    
    public int areaOfEffect;
    [Tooltip("Distance to target in which the projectile destroys itself.")]
    
    public float collisionRadius = 0.1f;
    [Tooltip("How much of an arc in the projectile's path.")]
    
    public float arcFactor = 0.5f; // Higher number means bigger arc.
    [Tooltip("Should the projectile always point towards target?")]
    
    public bool pointAtTarget;
    public Body sourceBody;
    private float radiusSq; // Radius squared; optimisation.
    private Transform target; // Who we are homing at.
    private Vector3 currentPosition; // Store the current position we are at.
    private float distanceTravelled; // Record the distance travelled.
    private Vector3 origin; // To store where the projectile first spawned.

    void OnEnable()
    {
        // Pre-compute the value. 
        radiusSq = collisionRadius * collisionRadius;
        origin = currentPosition = transform.position;
    }

    void Update()
    {
        // If there is no target, destroy itself and end execution.
        if (!target)
        {
            Destroy(gameObject);
            // Write your own code to spawn an explosion / splat effect.
            return; // Stops executing this function.
        }

        // Move ourselves towards the target at every frame.
        Vector3 direction = target.position - currentPosition;
        currentPosition += direction.normalized * speed * Time.deltaTime;
        distanceTravelled += speed * Time.deltaTime; // Record the distance we are travelling.

        // Set our position to <currentPosition>, and add a height offset to it.
        float totalDistance = Vector3.Distance(origin, target.position);
        float heightOffset = arcFactor * totalDistance * Mathf.Sin(distanceTravelled * Mathf.PI / totalDistance);
        Vector3 nextPosition = currentPosition + new Vector3(0, heightOffset, 0);
        
        if (pointAtTarget)
            transform.LookAt(nextPosition);
        
        transform.position = nextPosition;

        // Destroy the projectile if it is close to the target.
        if (direction.sqrMagnitude < radiusSq)
        {
            Destroy(gameObject);

            Body targetBody = target.GetComponent<Body>();            
            if (areaOfEffect == 0)
                targetBody.Damage(damage, AttributeChangeCause.ATTACKED, sourceBody, damageType);
            else
                foreach (Body body in World.GetBodiesInArea(target.position, areaOfEffect, areaOfEffect))
                {
                    if (!sourceBody.Faction.IsAllied(body.Faction))
                        body.Damage(damage, AttributeChangeCause.ATTACKED, sourceBody, damageType);
                }

            if (hitFx && targetBody)
                Instantiate(hitFx, target.position, hitFx.transform.rotation);
        }
    }

    // So that other scripts can use Projectile.Spawn to spawn a projectile.
    public static Projectile Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Body source, Transform target)
    {
        // Spawn a GameObject based on a prefab, and returns its Projectile component.
        GameObject go = Instantiate(prefab, position, rotation);
        Projectile p = go.GetComponent<Projectile>();

        // Rightfully, we should throw an error here instead of fixing the error for the user. 
        if (!p) p = go.AddComponent<Projectile>();

        // Set the projectile's target, so that it can work.
        p.target = target;
        p.sourceBody = source;
        return p;
    }
}
