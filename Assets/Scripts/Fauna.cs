using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;

public class Fauna : Actor
{    
    public float runSpeed;

    [Range(0.0f, 2.0f)]
    public float radius = 0.2f;
    public GameObject meatNodeToSpawnOnDeath;
    private Vector3 startPosition;
    private Animator animator;
    private float normalSpeed;
    public override void Initialize()
    {
        base.Initialize();
        startPosition = transform.position;
        normalSpeed = movementSpeed;
        animator = GetComponentInChildren<Animator>();
    }

    bool isRunnng;
    float timer;
    float actionTime;

    public override void Tick()
    {
        base.Tick();

        
        if (timer > actionTime)
        {
            int action = Random.Range(1, 100);
            isRunnng = false;

            // Eat/Idle
            if (action <= 40)
            {
                actionTime = Random.Range(0.05f, 0.15f);
            }
            // Walk
            else if (action <= 75)            
            {
                Vector3 randomPos = startPosition + (Vector3)(Random.insideUnitSphere * radius);
                Goto(World.ToWorldSpace(randomPos));
                actionTime = Random.Range(0.05f, 0.2f);
            }
            // Run
            else
            {
                Vector3 randomPos = startPosition + ((Vector3)Random.insideUnitCircle * radius);
                Goto(World.ToWorldSpace(randomPos));
                actionTime = Random.Range(0.05f, 0.2f);
                isRunnng = true;
            }

            timer = 0.0f;
            
        }
        
        animator.SetBool("Run", false);
        animator.SetBool("Walk", false);
        animator.SetBool("Eat", false);
        animator.SetBool("Turn Head", false);

        if (IsMoving())
        {
            if (isRunnng)
            {
                animator.SetBool("Run", true);
                movementSpeed = runSpeed;
            }
            else
            {
                movementSpeed = normalSpeed;
                animator.SetBool("Walk", true);
            }
        }
        else
        {
            if (Random.Range(1, 100) <= 50)
                animator.SetBool("Eat", true);
            else
                animator.SetBool("Turn Head", true);
        }

        timer += Time.deltaTime;
    }


    public void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(startPosition, radius);
    }
}
