using System.Collections;
using System.Collections.Generic;
using Swordfish.Navigation;
using UnityEngine;

public class Fauna : Actor
{
    public readonly static List<Fauna> AllFauna = new();

    public float runSpeed;

    [Tooltip("The radius that the actor will operate in centered on their starting spawn location.")]
    [Range(0.0f, 2.0f)]
    public float actionRadius = 0.5f;

    [Tooltip("Chance of a move action versus a non-movement based action occuring.")]
    [Range(0f, 1.0f)]
    public float moveActionChance = 0.5f;

    [Tooltip("Chance of a run action versus a walk action when a move action is occuring.")]
    [Range(0f, 1.0f)]
    public float runActionChance = 0.5f;

    [HideInInspector]
    [SerializeField]
    public float eatActionChance = 0.3f;
    [HideInInspector]
    [SerializeField]
    public float lookAroundActionChance = 0.6f;


    public GameObject liveFaunaObject;
    public GameObject deadFaunaObject;
    private Vector3 startPosition;
    private Animator animator;
    private float normalMovementSpeed;

    bool isRunnng;
    float newDecisionTimer;
    float actionTime;

    Resource resource;
    Fauna fauna;

    public override void Initialize()
    {
        base.Initialize();
        AllFauna.Add(this);
        startPosition = transform.position;
        normalMovementSpeed = movementSpeed;
        animator = GetComponentInChildren<Animator>();
        resource = GetComponent<Resource>();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        AllFauna.Remove(this);
    }

    enum FaunaActions
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (isDead || IsDead())
            return;

        if (newDecisionTimer > actionTime && !IsMoving())
        {

            isRunnng = false;

            MakeNewDecision();

            newDecisionTimer = 0.0f;

        }

        if (IsMoving())
        {
            if (isRunnng)
            {
                animator.SetInteger("FaunaActionState", (int)FaunaActions.Run);
                movementSpeed = runSpeed;
            }
            else
            {
                movementSpeed = normalMovementSpeed;
                animator.SetInteger("FaunaActionState", (int)FaunaActions.Walk);
            }
        }
        // Idle action
        else if (actionTime > 0)
        {
            float idleAction = Random.Range(0.0f, 1.0f);

            if (idleAction < eatActionChance)
            {
                // meatAmount += eatRate * Time.deltaTime;
                // float scale = meatAmount * 0.02f;
                // transform.localScale *= scale;
                animator.SetTrigger("Eat");
            }
            else if (idleAction < lookAroundActionChance)
                animator.SetTrigger("LookAround");
            else
                animator.SetInteger("FaunaActionState", (int)FaunaActions.Idle);
        }

        newDecisionTimer += Time.deltaTime;
    }

    // float meatAmount= 50.0f;
    // float eatRate = 0.01f;

    private bool isDead;
    public bool IsDead()
    {
        if (AttributeHandler.GetAttributePercent(Swordfish.Attributes.HEALTH) <= 0.0f)
        {
            isDead = true;
            resource.enabled = true;
            AttributeHandler.enabled = false;
            Freeze();
            ResetAI();
            liveFaunaObject.SetActive(false);
            deadFaunaObject.SetActive(true);
            animator.enabled = false;
        }

        return isDead;
    }

    void MakeNewDecision()
    {
        float action = Random.Range(0.0f, 1.0f);

        // Non-movement action
        if (action >= moveActionChance)
        {
            animator.SetInteger("FaunaActionState", (int)FaunaActions.Idle);
            actionTime = Random.Range(1.0f, 3.0f);
        }
        else
        {
            float moveAction = Random.Range(0.0f, 1.0f);
            if (moveAction <= runActionChance)
                RunAction();
            else
                WalkAction();
        }
    }

    void WalkAction()
    {
        GotoRandomPositionInRadius(actionRadius);
        actionTime = 0;
    }

    void RunAction()
    {
        GotoRandomPositionInRadius(actionRadius);
        actionTime = 0;
        isRunnng = true;
    }

    void GotoRandomPositionInRadius(float radius)
    {
        Vector3 randomPos = startPosition + ((Vector3)Random.insideUnitSphere * radius);

        Goto(World.ToWorldSpace(randomPos));
    }

    public override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        // if (Time.time > 0)
        //     startPosition = transform.position;

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(startPosition, Vector3.up, 1);
#endif
    }
}
