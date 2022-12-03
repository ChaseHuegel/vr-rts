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
    public float normalMovementSpeed;
    public float runSpeed;
    public float hitPoints = 10.0f;

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

    bool isRunnng;
    float newDecisionTimer;
    float actionTime;

    Resource resource;

    public override void IssueTargetedOrder(Body body)
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
        Attributes.Get(AttributeType.HEALTH).MaxValue = hitPoints;
        Attributes.Get(AttributeType.HEALTH).Value = hitPoints;
        Attributes.AddOrUpdate(AttributeType.SPEED, 1f);
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
                Attributes.Get(AttributeType.SPEED).Value = runSpeed;
                SetAnimatorsInteger("ActorAnimationState", (int)FaunaActions.RUN);
            }
            else
            {
                Attributes.Get(AttributeType.SPEED).Value = normalMovementSpeed;
                SetAnimatorsInteger("ActorAnimationState", (int)FaunaActions.WALK);
            }
        }
        // Idle action
        else if (actionTime > 0)
        {
            float idleAction = Random.Range(0.0f, 1.0f);

            if (idleAction < eatActionChance)
                SetAnimatorsTrigger(ActorAnimationTrigger.EAT);
            else if (idleAction < lookAroundActionChance)
                SetAnimatorsTrigger(ActorAnimationTrigger.LOOKAROUND);
            else
                SetAnimatorsInteger("ActorAnimationState", (int)FaunaActions.IDLE);
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
            deadFaunaObject.SetActive(true);
            DisableAnimators();
            Destroy(liveFaunaObject);
            Destroy(this);
        }

        return isDead;
    }

    protected override void OnDeath(DeathEvent e)
    {
        base.OnDeath(e);
        IsDead();
    }

    void MakeNewDecision()
    {
        float action = Random.Range(0.0f, 1.0f);

        // Non-movement action
        if (action >= moveActionChance)
        {
            SetAnimatorsInteger("ActorAnimationState", (int)FaunaActions.IDLE);
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
        IssueGoToOrder(World.ToWorldCoord(randomPos));
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(startPosition, Vector3.up, 1);

    }
#endif

}
