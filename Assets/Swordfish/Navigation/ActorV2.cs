using System;
using System.Collections.Generic;
using System.Linq;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Library.Types;
using UnityEditor;
using UnityEngine;
using Valve.VR.InteractionSystem;

namespace Swordfish.Navigation
{
    [RequireComponent(typeof(AudioSource))]
    public abstract class ActorV2 : Body, IActor
    {
        private const float InterpolationStrength = 1f - (Constants.ACTOR_PATH_RATE / 60f);

        public static List<ActorV2> AllActors { get; } = new List<ActorV2>();

        public List<Cell> CurrentPath
        {
            get => CurrentPathBinding.Get();
            set => CurrentPathBinding.Set(value);
        }

        public UnitOrder Order
        {
            get => OrderBinding.Get();
            set => OrderBinding.Set(value);
        }

        public ActorAnimationState State
        {
            get => StateBinding.Get();
            set => StateBinding.Set(value);
        }

        public Cell Destination
        {
            get => DestinationBinding.Get();
            set => DestinationBinding.Set(value);
        }

        public Body Target
        {
            get => TargetBinding.Get();
            set => TargetBinding.Set(value);
        }

        public bool Frozen
        {
            get => FrozenBinding.Get();
            set => FrozenBinding.Set(value);
        }

        public bool Falling
        {
            get => FallingBinding.Get();
            set => FallingBinding.Set(value);
        }

        public bool Held
        {
            get => HeldBinding.Get();
            set => HeldBinding.Set(value);
        }

        public bool IsMoving
        {
            get => IsMovingBinding.Get();
            private set => IsMovingBinding.Set(value);
        }

        public BehaviorTree<ActorV2> BehaviorTree { get; private set; }

        public bool OrderChangedRecently { get; private set; }
        public bool StateChangedRecently { get; private set; }
        public bool DestinationChangedRecently { get; private set; }
        public bool TargetChangedRecently { get; private set; }
        public bool PositionChangedRecently { get; private set; }

        public UnitOrder LastOrder { get; private set; }
        public ActorAnimationState LastState { get; private set; }
        public Cell LastDestination { get; private set; }
        public Body LastTarget { get; private set; }

        public DataBinding<List<Cell>> CurrentPathBinding { get; private set; } = new();
        public DataBinding<UnitOrder> OrderBinding { get; private set; } = new();
        public DataBinding<ActorAnimationState> StateBinding { get; private set; } = new();
        public DataBinding<Cell> DestinationBinding { get; private set; } = new();
        public DataBinding<Body> TargetBinding { get; private set; } = new();
        public DataBinding<bool> FrozenBinding { get; set; } = new();
        public DataBinding<bool> FallingBinding { get; private set; } = new();
        public DataBinding<bool> HeldBinding { get; private set; } = new();
        public DataBinding<bool> IsMovingBinding { get; private set; } = new();

        protected AudioSource AudioSource { get; private set; }
        protected Animator Animator { get; private set; }

        private byte PathWaitAttempts;
        private byte RepathAttempts;

        protected abstract BehaviorTree<ActorV2> BehaviorTreeFactory();

        public override void Initialize()
        {
            base.Initialize();
            AllActors.Add(this);

            AudioSource = GetComponent<AudioSource>();
            Animator = GetComponentInChildren<Animator>();

            BehaviorTree = BehaviorTreeFactory();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AllActors.Remove(this);
        }

        protected override void InitializeAttributes()
        {
            base.InitializeAttributes();
            Attributes.AddOrUpdate(AttributeConstants.SPEED, 0.3f, 0.3f);
            Attributes.AddOrUpdate(AttributeConstants.REACH, 1.5f);
            Attributes.AddOrUpdate(AttributeConstants.SENSE_RADIUS, 20f);
        }

        protected override void AttachListeners()
        {
            base.AttachListeners();
            FrozenBinding.Changed += OnFrozenChanged;
            StateBinding.Changed += OnStateChanged;
            DestinationBinding.Changed += OnDestinationChanged;
            TargetBinding.Changed += OnTargetChanged;
            OrderBinding.Changed += OnOrderChanged;
            FallingBinding.Changed += OnFallingChanged;
        }

        protected override void CleanupListeners()
        {
            base.CleanupListeners();
            FrozenBinding.Changed -= OnFrozenChanged;
            StateBinding.Changed -= OnStateChanged;
            DestinationBinding.Changed -= OnDestinationChanged;
            TargetBinding.Changed -= OnTargetChanged;
            OrderBinding.Changed -= OnOrderChanged;
            FallingBinding.Changed -= OnFallingChanged;
        }

        protected virtual void Update()
        {
            if (!Frozen)
                ProcessMovement(Time.deltaTime);
        }

        public override void Tick(float deltaTime)
        {
            if (StateChangedRecently)
                OnStateUpdate();

            StateChangedRecently = false;
            DestinationChangedRecently = false;
            TargetChangedRecently = false;
            OrderChangedRecently = false;
            PositionChangedRecently = false;
        }

        public abstract void IssueTargetedOrder(Body body);

        public void IssueGoToOrder(Coord2D coord) => IssueGoToOrder(World.at(coord));

        public void IssueGoToOrder(Cell cell)
        {
            Destination = cell;
            Order = UnitOrder.GoTo;
        }

        public void IssueSmartOrder(Cell cell)
        {
            Body body = cell.GetFirstOccupant();
            if (body)
                IssueTargetedOrder(body);
            else
                IssueGoToOrder(cell);
        }

        public void ResetPath()
        {
            CurrentPath = null;
            PathWaitAttempts = 0;
            RepathAttempts = 0;
        }

        public bool HasValidPath()
        {
            return CurrentPath != null && CurrentPath.Count > 0;
        }

        public void LookAt(float x, float y)
        {
            Vector3 temp = World.ToTransformSpace(new Vector3(x, 0, y));

            Vector3 lookPos = temp - transform.position;
            lookPos.y = 0;

            Quaternion rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = rotation;
        }

        protected virtual void OnTriggerEnter(Collider collider)
        {
            if (Falling)
            {
                Falling = false;
                Frozen = false;

                if (collider.TryGetComponent(out Body body))
                    OnFallingOntoBody(body);
            }
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (Falling)
            {
                Falling = false;
                Frozen = false;

                if (collision.relativeVelocity.magnitude > 4.0f)
                {
                    ContactPoint contact = collision.contacts[0];
                    float fallDamage = Vector3.Dot(contact.normal, collision.relativeVelocity) * 10f;
                    Damage(fallDamage, AttributeChangeCause.NATURAL, null, DamageType.BLUDGEONING);

                    AudioSource.PlayOneShot(GameMaster.GetAudio("unit_damaged").GetClip());
                }
            }
        }

        protected virtual void OnAttachedToHand(Hand hand)
        {
            Held = true;
        }

        protected virtual void OnDetachedFromHand(Hand hand)
        {
            Held = false;
        }

        protected virtual void AnimatorPlayAudio(string clipName)
        {
            AudioSource.PlayClipAtPoint(GameMaster.GetAudio(clipName).GetClip(), transform.position);
        }

        protected override void SyncToTransform()
        {
            base.SyncToTransform();
            ResetPath();
        }

        protected virtual void OnFallingOntoBody(Body body)
        {
        }

        protected virtual void OnStateUpdate()
        {
            Animator?.SetInteger("ActorAnimationState", (int)State);
        }

        protected virtual void OnFrozenChanged(object sender, DataChangedEventArgs<bool> e)
        {
            if (e.NewValue == true)
                RemoveFromGrid();
            else
                SyncToTransform();
        }

        protected virtual void OnStateChanged(object sender, DataChangedEventArgs<ActorAnimationState> e)
        {
            StateChangedRecently = true;
            LastState = e.OldValue;
        }

        protected virtual void OnDestinationChanged(object sender, DataChangedEventArgs<Cell> e)
        {
            DestinationChangedRecently = true;
            LastDestination = e.OldValue;
        }

        protected virtual void OnTargetChanged(object sender, DataChangedEventArgs<Body> e)
        {
            TargetChangedRecently = true;
            LastTarget = e.OldValue;
        }

        protected virtual void OnOrderChanged(object sender, DataChangedEventArgs<UnitOrder> e)
        {
            OrderChangedRecently = true;
            LastOrder = e.OldValue;
        }

        protected virtual void OnFallingChanged(object sender, DataChangedEventArgs<bool> e)
        {
            if (e.NewValue == false)
                transform.rotation = Quaternion.identity;
        }

        protected virtual void OnHeldChanged(object sender, DataChangedEventArgs<bool> e)
        {
            Falling = !e.NewValue;

            if (e.NewValue == true)
            {
                Frozen = true;
                State = ActorAnimationState.IDLE;
            }
        }

        private bool CanPathAhead()
        {
            if (!HasValidPath())
                return false;

            //  We can pass thru actors if the path ahead is clear and we are going beyond the next position.
            bool canPassThruActors = CurrentPath.Count > 2 && !World.at(CurrentPath[1].x, CurrentPath[1].y).IsBlocked();
            return CanSetPosition(CurrentPath[0].x, CurrentPath[0].y, canPassThruActors);
        }

        private void ProcessMovement(float deltaTime)
        {
            Vector3 gridToWorldSpace = World.ToTransformSpace(GridPosition.x, transform.position.y, GridPosition.y);

            IsMoving = Util.DistanceUnsquared(transform.position, gridToWorldSpace) > 0.001f;

            if (IsMoving)
            {
                State = ActorAnimationState.MOVING;

                transform.position = Vector3.MoveTowards(
                        transform.position,
                        gridToWorldSpace,
                        deltaTime * InterpolationStrength * Attributes.ValueOf(AttributeConstants.SPEED)
                    );
            }
            else
            {
                if (State == ActorAnimationState.MOVING && !CanPathAhead())
                    State = ActorAnimationState.IDLE;

                ProcessPathing();
            }
        }

        private void ProcessPathing()
        {
            //  Ensure we're facing our target if we aren't moving
            if (!IsMoving && Target != null && !HasValidPath())
                LookAt(Target.GetPosition().x, Target.GetPosition().y);

            //  If we have a valid path, move along it.
            if (HasValidPath())
            {
                // TODO: Add 'waypoints' for longer paths too big for the heap

                //  Attempt to move to the next point
                if (CanPathAhead())
                {
                    //  Reset pathing logic
                    PathWaitAttempts = 0;
                    RepathAttempts = 0;

                    //  Only process the move if we finished interpolating
                    if (!IsMoving)
                    {
                        //  Look in the direction we're going
                        LookAt(CurrentPath[0].x, CurrentPath[0].y);

                        SetPosition(CurrentPath[0].x, CurrentPath[0].y);
                        CurrentPath.RemoveAt(0);

                        PositionChangedRecently = true;
                    }
                }
                //  Unable to reach the next point, handle pathing logic on tick
                else
                {
                    // Wait some time to see if path clears
                    if (PathWaitAttempts > Constants.PATH_WAIT_TRIES)
                    {
                        //  Path hasn't cleared, try repathing to a point near the current node or occupant of node
                        if (RepathAttempts < Constants.PATH_REPATH_TRIES)
                        {
                            int targetX, targetY;

                            if (Target != null)
                            {
                                Coord2D coord = Target.GetRandomAdjacentPosition();
                                targetX = coord.x;
                                targetY = coord.y;
                            }
                            else
                            {
                                targetX = CurrentPath[^1].x + UnityEngine.Random.Range(-1, 1);
                                targetY = CurrentPath[^1].y + UnityEngine.Random.Range(-1, 1);
                            }

                            //  false, dont ignore actors. Stuck and may need to path around them
                            PathManager.RequestPath(this, targetX, targetY, false);
                        }
                        //  Unable to repath
                        else
                        {
                            //  Reset pathing
                            ResetPath();
                            Destination = null;
                            Target = null;
                        }

                        RepathAttempts++;
                    }

                    PathWaitAttempts++;
                }
            }
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            if (CurrentPath != null)
            {
                foreach (Cell cell in CurrentPath)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(World.ToTransformSpace(new Vector3(cell.x, 0, cell.y)), 0.25f * World.GetUnit());
                }
            }

            if (LastTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(World.ToTransformSpace(LastTarget.GetPosition()), 0.25f * World.GetUnit());
            }

            if (Target != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(World.ToTransformSpace(Target.GetPosition()), 0.25f * World.GetUnit());

                if (Target.GetCell().GetFirstOccupant() != null)
                {
                    Coord2D coord = Target.GetCell().GetFirstOccupant().GetNearestPositionFrom(GridPosition);

                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(World.ToTransformSpace(coord.x, coord.y), 0.25f * World.GetUnit());
                }
            }
        }

        private Vector2 behaviorTreeScrollPosition = Vector2.zero;
        protected virtual void OnGUI()
        {
            if (!Selection.gameObjects.Contains(gameObject))
                return;

            if (BehaviorTree != null)
            {
                behaviorTreeScrollPosition = GUI.BeginScrollView(
                    new Rect(0, 100, 400, Display.main.renderingHeight - 100),
                    behaviorTreeScrollPosition,
                    new Rect(0, 0, 600, 6000),
                    true,
                    true
                );

                DrawBehaviorTreeRecursively(BehaviorTree.Root);
                GUI.EndScrollView();
            }
        }

        private void DrawBehaviorTreeRecursively(BehaviorNode node)
        {
            int levels = 0;
            for (int i = 0; i < node.Children.Count; i++)
                levels += 1 + DrawBehaviorTreeBranch(node.Children[i], 0, levels);
        }

        private int DrawBehaviorTreeBranch(BehaviorNode node, int depth, int level)
        {
            const int width = 200;
            const int height = 30;
            const int indentation = 20;
            const int spacing = height + 4;

            Color color = Color.white;
            switch (node)
            {
                case IBehaviorAction _:
                    color = Color.cyan;
                    break;
                case IBehaviorCompositor _:
                    color = Color.magenta;
                    break;
                case IBehaviorCondition _:
                    color = Color.yellow;
                    break;
                case IBehaviorDecorator _:
                    color = Color.red;
                    break;
            }

            GUI.color = color;
            GUI.Box(new Rect(indentation * depth, spacing * level, width, height), node.GetType().Name);

            int levels = 0;
            for (int i = 0; i < node.Children.Count; i++)
            {
                levels += DrawBehaviorTreeBranch(node.Children[i], depth + 1, levels + level + i + 1);
            }

            return node.Children.Count + levels;
        }
#endif
    }
}