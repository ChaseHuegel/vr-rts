using System.Collections;
using System.Collections.Generic;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Navigation;
using UnityEngine;

public class Fauna : ActorV2
{
    enum FaunaActions
    {
        IDLE = 0,
        WALK = 1,
        RUN = 2,
    }

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
    private float normalMovementSpeed;

    bool isRunnng;
    float newDecisionTimer;
    float actionTime;

    Resource resource;

    public override void OrderToTarget(Body body)
    {
        Target = body;
    }

    protected override BehaviorTree<ActorV2> BehaviorTreeFactory()
    {
        return FaunaBehaviorTree.Get();
    }

    public override void Initialize()
    {
        base.Initialize();
        AllFauna.Add(this);
        startPosition = transform.position;
        resource = GetComponent<Resource>();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        AllFauna.Remove(this);
    }

    protected override void InitializeAttributes()
    {
        base.InitializeAttributes();
        Attributes.AddOrUpdate(AttributeConstants.HEALTH, 25f, 25f);
        normalMovementSpeed = Attributes.ValueOf(AttributeConstants.SPEED);
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (isDead || IsDead())
            return;

        if (newDecisionTimer > actionTime && !IsMoving)
        {
            isRunnng = false;
            MakeNewDecision();
            newDecisionTimer = 0.0f;
        }

        if (IsMoving)
        {
            if (isRunnng)
            {
                Attributes.Get(AttributeConstants.SPEED).Value = runSpeed;
                Animator.SetInteger("FaunaActionState", (int)FaunaActions.RUN);
            }
            else
            {
                Attributes.Get(AttributeConstants.SPEED).Value = normalMovementSpeed;
                Animator.SetInteger("FaunaActionState", (int)FaunaActions.WALK);
            }
        }
        // Idle action
        else if (actionTime > 0)
        {
            float idleAction = Random.Range(0.0f, 1.0f);

            if (idleAction < eatActionChance)
                Animator.SetTrigger("Eat");
            else if (idleAction < lookAroundActionChance)
                Animator.SetTrigger("LookAround");
            else
                Animator.SetInteger("FaunaActionState", (int)FaunaActions.IDLE);
        }

        newDecisionTimer += Time.deltaTime;
    }

    private bool isDead;
    public bool IsDead()
    {
        if (!IsAlive())
        {
            isDead = true;
            resource.enabled = true;
            Frozen = true;
            liveFaunaObject.SetActive(false);
            deadFaunaObject.SetActive(true);
            Animator.enabled = false;
        }

        return isDead;
    }

    void MakeNewDecision()
    {
        float action = Random.Range(0.0f, 1.0f);

        // Non-movement action
        if (action >= moveActionChance)
        {
            Animator.SetInteger("FaunaActionState", (int)FaunaActions.IDLE);
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
        Vector3 randomPos = startPosition + (Random.insideUnitSphere * radius);
        OrderGoTo(World.ToWorldCoord(randomPos));
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(startPosition, Vector3.up, 1);
#endif
    }
}
