using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 8.5f; // Speed of projectile.
    public float radius = 1f; // Collision radius.
    float radiusSq; // Radius squared; optimisation.
    Transform target; // Who we are homing at.

    Vector3 currentPosition; // Store the current position we are at.
    float distanceTravelled; // Record the distance travelled.

    public float arcFactor = 0.5f; // Higher number means bigger arc.
    Vector3 origin; // To store where the projectile first spawned.

    void OnEnable()
    {
        // Pre-compute the value. 
        radiusSq = radius * radius;
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
        transform.position = currentPosition + new Vector3(0, heightOffset, 0);

        // Destroy the projectile if it is close to the target.
        if (direction.sqrMagnitude < radiusSq)
        {
            Destroy(gameObject);
            // Write your own code to spawn an explosion / splat effect.
            // Write your own code to deal damage to the .
        }
    }

    // So that other scripts can use Projectile.Spawn to spawn a projectile.
    public static Projectile Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform target)
    {
        // Spawn a GameObject based on a prefab, and returns its Projectile component.
        GameObject go = Instantiate(prefab, position, rotation);
        Projectile p = go.GetComponent<Projectile>();

        // Rightfully, we should throw an error here instead of fixing the error for the user. 
        if (!p) p = go.AddComponent<Projectile>();

        // Set the projectile's target, so that it can work.
        p.target = target;
        return p;
    }
}
