using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{

public class Actor : Body
{
    protected GoalHolder goals = new GoalHolder();
    public PathfindingGoal[] GetGoals() { return goals.entries; }

    private Damageable damageable;
    public Damageable AttributeHandler { get { return damageable; } }

    [Header("Actor")]
    public float movementSpeed = 1f;

    [SerializeField] private byte goalSearchDistance = 20;
    private byte goalSearchGrowth;
    private byte currentGoalSearchDistance;

    private Cell currentGoalTarget = null;
    private PathfindingGoal currentGoal = null;

    private float movementInterpolation;
    private bool moving = false;
    private bool idle = false;

    public List<Cell> currentPath = null;
    private byte pathWaitTries = 0;
    private byte pathRepathTries = 0;
    private bool frozen = false;
    private bool isPathLocked = false;

    private byte pathTimer = 0;
    private byte tickTimer = 0;

    public override void Initialize()
    {
        base.Initialize();

        if (!(damageable = GetComponent<Damageable>()))
            Debug.Log("Damageable component not found.");

        movementInterpolation = 1f - (Constants.ACTOR_PATH_RATE / 60f);

        goalSearchGrowth = (byte)(goalSearchDistance * 0.25f);
        currentGoalSearchDistance = goalSearchGrowth;
    }


#region immutable methods

    public bool IsIdle() { return idle; }
    private void UpdateIdle()
    {
        //  Idle if not frozen and not moving, pathing, or has a target goal
        idle = ( !frozen && !(IsMoving() || HasValidPath() || HasValidTarget()) );
    }

    public bool IsMoving() { return moving; }
    public bool HasValidPath() { return (currentPath != null && currentPath.Count > 0); }

    public bool HasValidGoal() { return (currentGoal != null && currentGoal.active); }
    public bool HasValidGoalTarget() { return currentGoalTarget != null; }

    public bool HasValidTarget()
    {
        return (HasValidGoal() && HasValidGoalTarget() && PathfindingGoal.CheckGoal(this, currentGoalTarget, currentGoal));
    }

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
        ResetAI();
    }

    public void ResetGoal()
    {
        currentGoal = null;
        currentGoalTarget = null;
    }

    public void ResetPathingBrain()
    {
        pathWaitTries = 0;
        pathRepathTries = 0;
    }

    public void ResetPath()
    {
        currentPath = null;
    }

    public void ResetAI()
    {
        ResetPath();
        ResetGoal();
        ResetPathingBrain();
    }

    public void TryGoalAtHelper(int relativeX, int relativeY, PathfindingGoal goal, ref Cell current, ref Cell result, ref int currentDistance, ref int nearestDistance)
    {
        current = World.at(gridPosition.x + relativeX, gridPosition.y + relativeY);
        currentDistance = DistanceTo(current);

        if (PathfindingGoal.TryGoal(this, current, goal) && currentDistance < nearestDistance)
        {
            nearestDistance = currentDistance;
            result = current;
        }
    }

    public Cell FindNearestGoalWithPriority() { return FindNearestGoal(true); }
    public Cell FindNearestGoal(bool usePriority = false)
    {
        Cell result = null;
        Cell current = null;

        int currentDistance = 0;
        int nearestDistance = int.MaxValue;

        foreach (PathfindingGoal goal in GetGoals())
        {
            currentGoal = goal;

            //  TODO: There is a cleaner way to do this

            //  Radiate out layer by layer around the actor without searching previous layers
            for (int radius = 1; radius < currentGoalSearchDistance; radius++)
            {
                //  Search the top/bottom rows
                for (int x = -radius; x < radius; x++)
                {
                    TryGoalAtHelper(x, radius, goal, ref current, ref result, ref currentDistance, ref nearestDistance);
                    TryGoalAtHelper(x, -radius, goal, ref current, ref result, ref currentDistance, ref nearestDistance);

                    //  Return the first match if goals are being tested in order of priority
                    if (usePriority && result != null)
                    {
                        currentGoalSearchDistance = goalSearchGrowth;  //  Reset search distance
                        return result;
                    }
                }

                //  Search the side columns
                for (int y = -radius; y < radius; y++)
                {
                    TryGoalAtHelper(radius, y, goal, ref current, ref result, ref currentDistance, ref nearestDistance);
                    TryGoalAtHelper(-radius, y, goal, ref current, ref result, ref currentDistance, ref nearestDistance);

                    //  Return the first match if goals are being tested in order of priority
                    if (usePriority && result != null)
                    {
                        currentGoalSearchDistance = goalSearchGrowth;  //  Reset search distance
                        return result;
                    }
                }
            }
        }

        //  No matching goal found
        if (result == null)
            currentGoal = null;

        //  Expand the search
        currentGoalSearchDistance = (byte)Mathf.Clamp(currentGoalSearchDistance + goalSearchGrowth, 1, goalSearchDistance);

        return result;
    }

    public bool GotoNearestGoalWithPriority() { return GotoNearestGoal(true); }
    public bool GotoNearestGoal(bool usePriority = false)
    {
        if (isPathLocked) return false;

        if (!HasValidTarget())
            currentGoalTarget = FindNearestGoal(usePriority);

        if (HasValidTarget())
        {
            Goto(currentGoalTarget.x, currentGoalTarget.y);
            return true;
        }

        return false;
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

        // float damping = 1.0f;
        var lookPos = temp - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = rotation;// Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * damping);
    }
#endregion


#region monobehavior

    //  Perform ticks at a regular interval. FixedUpdate is called 60x/s
    public void FixedUpdate()
    {
        //  Behavior ticking below
        tickTimer++;
        if (tickTimer >= Constants.ACTOR_TICK_RATE)
        {
            tickTimer = 0;

            //  Handle interacting with goals
            if (!moving && HasValidTarget())
            {
                //  Check if we have reached our target, or the path ahead matches our goal
                if (DistanceTo(currentGoalTarget) <= 1 || (HasValidPath() && PathfindingGoal.CheckGoal(this, currentPath[0], currentGoal)))
                {
                    //  Assume our currentGoal is a valid match since it was found successfully.
                    //  Forcibly trigger reached under that assumption
                    PathfindingGoal.TriggerInteractGoal(this, currentGoalTarget, currentGoal);

                    if (HasValidGoalTarget())
                        LookAt(currentGoalTarget.x, currentGoalTarget.y);
                    else if (HasValidPath())
                        LookAt(currentPath[0].x, currentPath[0].y);

                    ResetPathingBrain();
                    ResetPath();
                }
            }

            UpdateIdle();
            if (IsIdle()) ResetAI();

            Tick();
        }

        //  Pathfinding and interpolation below
        //  Don't pathfind while frozen
        if (frozen) return;

        Vector3 gridTransformPos = World.ToTransformSpace(gridPosition.x, transform.position.y, gridPosition.y);
        bool reachedTarget = true;

        moving = false;

        //  Interpolate movement if the transform hasnt reached our world space grid pos
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
                reachedTarget = false;
        }

        pathTimer++;
        if (pathTimer >= Constants.ACTOR_PATH_RATE)
            pathTimer = 0;  //  Ticked

        //  Make certain pathing is unlocked as soon as the path is no longer valid
        if (IsPathLocked() && !HasValidPath())
            UnlockPath();

        //  If we have a valid path, move along it
        if (HasValidPath())
        {
            // TODO: Add 'waypoints' for longer paths too big for the heap

            //  We can pass thru actors if the path ahead is clear and we are going beyond the next spot
            bool canPassThruActors = currentPath.Count > 2 ? !World.at(currentPath[1].x, currentPath[1].y).IsBlocked() : false;

            //  Attempt to move to the next point
            if ( CanSetPosition(currentPath[0].x, currentPath[0].y, canPassThruActors) )
            {
                //  If the path is clear, reset pathing logic
                ResetPathingBrain();

                //  Only move if we finished interpolating
                if (reachedTarget)
                {
                    //  Look in the direction we're going
                    LookAt(currentPath[0].x, currentPath[0].y);
                    SetPositionUnsafe(currentPath[0].x, currentPath[0].y);

                    currentPath.RemoveAt(0);
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
                            Coord2D coord = currentGoalTarget.GetFirstOccupant().GetNearbyCoord();
                            targetX = coord.x;
                            targetY = coord.y;
                        }
                        else
                        {
                            targetX = currentPath[currentPath.Count - 1].x + UnityEngine.Random.Range(-1, 1);
                            targetY = currentPath[currentPath.Count - 1].y + UnityEngine.Random.Range(-1, 1);
                        }

                        //  false, dont ignore actors. Stuck and may need to path around them
                        GotoForced(targetX, targetY, false);

                        //  Cycle goals to change priorities
                        goals.Cycle();

                        //  Trigger repath event
                        RepathEvent e = new RepathEvent{ actor = this };
                        OnRepathEvent?.Invoke(null, e);
                        if (e.cancel) ResetPathingBrain();   //  Reset pathing logic if cancelled
                    }
                    //  Unable to repath
                    else
                    {
                        //  Reset pathing
                        ResetAI();

                        //  Trigger repath failed event
                        RepathFailedEvent e = new RepathFailedEvent{ actor = this };
                        OnRepathFailedEvent?.Invoke(null, e);
                    }

                    pathRepathTries++;
                }

                pathWaitTries++;
            }
        }
    }

    //  Debug drawing
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
#endregion

#region events

    public static event EventHandler<RepathEvent> OnRepathEvent;
    public class RepathEvent : Swordfish.Event
    {
        public Actor actor;
    }

    public static event EventHandler<RepathFailedEvent> OnRepathFailedEvent;
    public class RepathFailedEvent : Swordfish.Event
    {
        public Actor actor;
    }
#endregion
}

}