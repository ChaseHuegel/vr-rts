using System.Collections.Generic;
using System.Linq;
using Swordfish.Library.BehaviorTrees;
using UnityEditor;
using UnityEngine;

namespace Swordfish.Navigation
{
    [RequireComponent(typeof(Damageable))]
    public abstract class ActorV2 : Body, IActor
    {
        protected abstract BehaviorTree<ActorV2> BehaviorTree { get; set; }

        public UnitOrder Order;

        public Cell LastDestination;
        public Cell Destination
        {
            get => destination;
            set
            {
                LastDestination = destination;
                destination = value;
            }
        }
        private Cell destination;

        public Body LastTarget;
        public Body Target
        {
            get => target;
            set
            {
                LastTarget = target;
                target = value;
            }
        }
        [SerializeField] private Body target;

        private Damageable damageable;
        public Damageable AttributeHandler { get { return damageable; } }

        [Header("Actor")]
        public float movementSpeed = 1f;

        public int InteractReach = 1;

        private float movementInterpolation;
        private bool moving = false;
        private bool idle = false;

        public List<Cell> currentPath { get; set; } = null;
        private bool frozen = false;
        private bool isPathLocked = false;
        private byte pathWaitTries = 0;
        private byte pathRepathTries = 0;
        private byte pathTimer = 0;

        public override void Initialize()
        {
            base.Initialize();

            if (!(damageable = GetComponent<Damageable>()))
                Debug.Log("Damageable component not found.");

            movementInterpolation = 1f - (Constants.ACTOR_PATH_RATE / 60f);
        }

        public bool IsIdle() { return idle; }
        private bool UpdateIdle()
        {
            //  Idle if not frozen and not moving, pathing, or has a target goal
            return idle = !frozen && !(IsMoving() || HasValidPath() || HasValidTarget());
        }

        private bool HasValidTarget()
        {
            return false;
        }

        public bool IsMoving() { return moving; }
        public bool HasValidPath() { return currentPath != null && currentPath.Count > 0; }
        public bool HasDestinationChanged() => Destination != LastDestination;
        public bool HasTargetChanged() => Target != LastTarget;

        public void Freeze() { frozen = true; RemoveFromGrid(); }
        public void Unfreeze() { frozen = false; UpdatePosition(); }
        public void ToggleFreeze()
        {
            if (frozen = !frozen == false) UpdatePosition();
        }

        public bool IsPathLocked() { return isPathLocked; }
        public void LockPath() { isPathLocked = true; }
        public void UnlockPath() { isPathLocked = false; }

        public void UpdatePosition()
        {
            SyncPosition();
            ResetPath();
        }

        public void ResetPath()
        {
            currentPath = null;
        }

        public void Goto(Direction dir, int distance, bool ignoreActors = true) { Goto(dir.toVector3() * distance, ignoreActors); }
        public void Goto(Coord2D coord, bool ignoreActors = true) { Goto(coord.x, coord.y, ignoreActors); }
        public void Goto(Vector2 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.y, ignoreActors); }
        public void Goto(Vector3 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.z, ignoreActors); }
        public void Goto(int x, int y, bool ignoreActors = true)
        {
            if (!isPathLocked && !HasValidPath() && DistanceTo(x, y) > 1)
                PathManager.RequestPath(this, x, y, ignoreActors);
        }

        public void GotoForced(Direction dir, int distance, bool ignoreActors = true) { Goto(dir.toVector3() * distance, ignoreActors); }
        public void GotoForced(Coord2D coord, bool ignoreActors = true) { Goto(coord.x, coord.y, ignoreActors); }
        public void GotoForced(Vector2 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.y, ignoreActors); }
        public void GotoForced(Vector3 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.z, ignoreActors); }
        public void GotoForced(int x, int y, bool ignoreActors = true)
        {
            if (!isPathLocked && DistanceTo(x, y) > 1)
                PathManager.RequestPath(this, x, y, ignoreActors);
        }

        public void LookAt(float x, float y)
        {
            Vector3 temp = World.ToTransformSpace(new Vector3(x, 0, y));

            var lookPos = temp - transform.position;
            lookPos.y = 0;
            var rotation = Quaternion.LookRotation(lookPos);
            transform.rotation = rotation;
        }

        public void FixedUpdate()
        {
            if (BehaviorTree != null)
                BehaviorTree.Tick(this, Time.fixedDeltaTime);

            //  Pathfinding and interpolation below
            //  Don't pathfind while frozen
            if (frozen) return;

            Vector3 gridTransformPos = World.ToTransformSpace(gridPosition.x, transform.position.y, gridPosition.y);

            //  Interpolate movement if the transform hasn't reached our world space grid pos
            moving = false;
            bool finishedInterpolating = true;
            if (Util.DistanceUnsquared(transform.position, gridTransformPos) > 0.001f)
            {
                moving = true;

                transform.position = Vector3.MoveTowards
                (
                    transform.position,
                    gridTransformPos,
                    Time.fixedDeltaTime * movementInterpolation * movementSpeed
                );

                if (World.ToWorldCoord(transform.position) != gridPosition)
                    finishedInterpolating = false;
            }

            pathTimer++;
            if (pathTimer >= Constants.ACTOR_PATH_RATE)
                pathTimer = 0;  //  Ticked

            //  Make certain pathing is unlocked as soon as the path is no longer valid
            if (IsPathLocked() && !HasValidPath() || HasDestinationChanged())
                UnlockPath();

            //  If we have a valid path, move along it
            if (HasValidPath())
            {
                // TODO: Add 'waypoints' for longer paths too big for the heap

                //  We can pass thru actors if the path ahead is clear and we are going beyond the next spot
                bool canPassThruActors = currentPath.Count > 2 && !World.at(currentPath[1].x, currentPath[1].y).IsBlocked();

                //  Attempt to move to the next point
                if (CanSetPosition(currentPath[0].x, currentPath[0].y, canPassThruActors))
                {
                    //  If the path is clear, reset pathing logic
                    pathWaitTries = 0;
                    pathRepathTries = 0;

                    //  Only move if we finished interpolating
                    if (finishedInterpolating)
                    {
                        //  Look in the direction we're going
                        LookAt(currentPath[0].x, currentPath[0].y);

                        SetPositionUnsafe(currentPath[0].x, currentPath[0].y);
                        currentPath.RemoveAt(0);

                        //  Make certain the path is unlocked once the end is reached
                        if (currentPath.Count == 0)
                            UnlockPath();
                    }
                }
                //  Unable to reach the next point, handle pathing logic on tick
                else if (pathTimer == 0)
                {
                    // Wait some time to see if path clears
                    if (pathWaitTries > Constants.PATH_WAIT_TRIES)
                    {
                        //  Path hasn't cleared, try repathing to a point near the current node or occupant of node
                        if (pathRepathTries < Constants.PATH_REPATH_TRIES)
                        {
                            int targetX, targetY;

                            if (HasValidTarget())
                            {
                                Coord2D coord = Destination.GetFirstOccupant().GetNearbyCoord();
                                targetX = coord.x;
                                targetY = coord.y;
                            }
                            else
                            {
                                targetX = currentPath[^1].x + Random.Range(-1, 1);
                                targetY = currentPath[^1].y + Random.Range(-1, 1);
                            }

                            //  false, dont ignore actors. Stuck and may need to path around them
                            GotoForced(targetX, targetY, false);
                        }
                        //  Unable to repath
                        else
                        {
                            //  Reset pathing
                            ResetPath();
                            pathWaitTries = 0;
                            pathRepathTries = 0;
                        }

                        pathRepathTries++;
                    }

                    pathWaitTries++;
                }
            }
        }

        public virtual void OnDrawGizmosSelected()
        {
            if (Application.isEditor != true) return;

            if (currentPath != null)
            {
                foreach (Cell cell in currentPath)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(World.ToTransformSpace(new Vector3(cell.x, 0, cell.y)), 0.25f * World.GetUnit());
                }
            }
        }

        private Vector2 behaviorTreeScrollPosition = Vector2.zero;
        public virtual void OnGUI()
        {
            if (Application.isEditor != true || !Selection.gameObjects.Contains(gameObject)) return;

            if (BehaviorTree != null)
            {
                behaviorTreeScrollPosition = GUI.BeginScrollView(
                    new Rect(0, 0, 500, Display.main.renderingHeight),
                    behaviorTreeScrollPosition,
                    new Rect(0, 0, 500, 500),
                    true,
                    true
                );

                DrawBehaviorTreeRecursively(BehaviorTree.Root, 0, 0);
                GUI.EndScrollView();
            }
        }

        private void DrawBehaviorTreeRecursively(BehaviorNode node, int depth, int level)
        {
            const int width = 200;
            const int height = 30;
            const int indentation = 20;
            const int spacing = height + 4;

            GUI.Box(new Rect(indentation * depth, spacing * level, width, height), node.GetType().Name);

            for (int i = 0; i < node.Children.Count; i++)
            {
                DrawBehaviorTreeRecursively(node.Children[i], depth + 1, level + i + 1);
            }
        }
    }

}