﻿using System;
using System.Collections.Generic;
using System.Linq;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Library.Types;
using UnityEditor;
using UnityEngine;

namespace Swordfish.Navigation
{
    [RequireComponent(typeof(Damageable))]
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

        public bool IsMoving
        {
            get => IsMovingBinding.Get();
            set => IsMovingBinding.Set(value);
        }

        public bool Frozen
        {
            get => FrozenBinding.Get();
            set => FrozenBinding.Set(value);
        }

        public abstract BehaviorTree<ActorV2> BehaviorTree { get; protected set; }
        public abstract float Speed { get; protected set; }
        public abstract int Reach { get; protected set; }

        public Animator Animator { get; private set; }
        public Damageable AttributeHandler { get; private set; }

        public bool StateChangedRecently { get; private set; }
        public bool DestinationChangedRecently { get; private set; }
        public bool TargetChangedRecently { get; private set; }

        public ActorAnimationState LastState { get; private set; }
        public Cell LastDestination { get; private set; }
        public Body LastTarget { get; private set; }

        public DataBinding<List<Cell>> CurrentPathBinding { get; private set; } = new();
        public DataBinding<UnitOrder> OrderBinding { get; private set; } = new();
        public DataBinding<ActorAnimationState> StateBinding { get; private set; } = new();
        public DataBinding<Cell> DestinationBinding { get; private set; } = new();
        public DataBinding<Body> TargetBinding { get; private set; } = new();
        public DataBinding<bool> IsMovingBinding { get; private set; } = new();
        public DataBinding<bool> FrozenBinding { get; set; } = new();

        private byte PathWaitAttempts = 0;
        private byte RepathAttempts = 0;

        public override void Initialize()
        {
            base.Initialize();
            AllActors.Add(this);

            Animator = GetComponentInChildren<Animator>();
            AttributeHandler = GetComponent<Damageable>();

            FrozenBinding.Changed += OnFrozenChanged;
            StateBinding.Changed += OnStateChanged;
            DestinationBinding.Changed += OnDestinationChanged;
            TargetBinding.Changed += OnTargetChanged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AllActors.Remove(this);

            FrozenBinding.Changed -= OnFrozenChanged;
            StateBinding.Changed -= OnStateChanged;
            DestinationBinding.Changed -= OnDestinationChanged;
            TargetBinding.Changed -= OnTargetChanged;
        }

        protected virtual void Update()
        {
            if (!Frozen)
                ProcessMovement(Time.deltaTime);
        }

        public override void Tick(float deltaTime)
        {
            if (StateChangedRecently)
                Animator.SetInteger("ActorAnimationState", (int)State);

            StateChangedRecently = false;
            DestinationChangedRecently = false;
            TargetChangedRecently = false;
        }

        public override void SyncToTransform()
        {
            base.SyncToTransform();
            ResetPath();
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

        private void OnFrozenChanged(object sender, DataChangedEventArgs<bool> e)
        {
            if (e.NewValue == true)
                RemoveFromGrid();
            else
                SyncToTransform();
        }

        private void OnStateChanged(object sender, DataChangedEventArgs<ActorAnimationState> e)
        {
            StateChangedRecently = true;
            LastState = e.OldValue;
        }

        private void OnDestinationChanged(object sender, DataChangedEventArgs<Cell> e)
        {
            DestinationChangedRecently = true;
            LastDestination = e.OldValue;
        }

        private void OnTargetChanged(object sender, DataChangedEventArgs<Body> e)
        {
            TargetChangedRecently = true;
            LastTarget = e.OldValue;
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
                        deltaTime * InterpolationStrength * Speed
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