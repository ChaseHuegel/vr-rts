using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{

[RequireComponent(typeof(Damageable))]
public class Actor : Body
{
    protected GoalHolder goals = new GoalHolder();
    public PathfindingGoal[] GetGoals() { return goals.entries; }

    private Damageable damageable;
    public Damageable AttributeHandler { get { return damageable; } }

    [Header("Actor")]
    public bool doMicroSearching = false;
    public float movementSpeed = 1f;

    [SerializeField] private byte goalSearchDistance = 20;
    private byte goalSearchGrowth;
    private byte currentGoalSearchDistance;
    protected int maxGoalInteractRange = 1;

    protected Cell currentGoalCell = null;
    protected Cell previousGoalCell = null;
    protected PathfindingGoal currentGoal = null;
    protected PathfindingGoal previousGoal = null;

    //  Memory functionality
    protected Dictionary<PathfindingGoal, Cell> discoveredGoals = new Dictionary<PathfindingGoal, Cell>();

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
    private bool UpdateIdle()
    {
        //  Idle if not frozen and not moving, pathing, or has a target goal
        return idle = ( !frozen && !(IsMoving() || HasValidPath() || HasValidTarget()) );
    }

    public bool IsMoving() { return moving; }
    public bool HasValidPath() { return (currentPath != null && currentPath.Count > 0); }

    public bool HasValidGoal() { return (currentGoal != null && currentGoal.active); }
    public bool HasValidGoalTarget() { return currentGoalCell != null; }

    public bool HasValidTarget()
    {
        return (HasValidGoal() && HasValidGoalTarget() && PathfindingGoal.CheckGoal(this, currentGoalCell, currentGoal));
    }

    public bool HasTargetChanged()
    {
        return currentGoalCell != previousGoalCell;
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
        previousGoal = null;
        currentGoal = null;
        currentGoalCell = null;
        previousGoalCell = null;
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

    //  Shouldn't be used outside of core actor logic
    private void ResetMemory()
    {
        discoveredGoals.Clear();
    }

    //  Shouldn't be used outside of core actor logic
    private void WipeAI()
    {
        ResetAI();
        ResetMemory();
    }

    public void TryGoalAtHelper(int relativeX, int relativeY, PathfindingGoal goal, ref Cell current, ref Cell result, ref int currentDistance, ref int nearestDistance)
    {
        current = World.at(gridPosition.x + relativeX, gridPosition.y + relativeY);
        currentDistance = DistanceTo(current);

        if (currentDistance < nearestDistance && PathfindingGoal.TryGoal(this, current, goal))
        {
            nearestDistance = currentDistance;
            result = current;
        }
    }

    public void TryDiscoverGoal(PathfindingGoal goal, Cell cell)
    {
        if (goal == null || cell == null) return;

        if (discoveredGoals.ContainsKey(goal))
            discoveredGoals.Remove(goal);

        discoveredGoals.Add(goal, cell);
    }

    public Cell FindNearestGoalWithPriority(bool useBehavior = true) { return FindNearestGoal(true, useBehavior); }
    public Cell FindNearestGoal(bool usePriority = false, bool useBehavior = true)
    {
        Cell result = null;
        Cell current = null;

        int currentDistance = 0;
        int nearestDistance = int.MaxValue;
        int searchDistance = useBehavior ? currentGoalSearchDistance : goalSearchGrowth;

        //  If using priority, try checking our memorized goals first
        if (useBehavior && usePriority && discoveredGoals.Count > 0)
        {
            foreach (PathfindingGoal goal in GetGoals())
            {
                currentGoal = goal;

                if (discoveredGoals.TryGetValue(goal, out result)
                    && result != null
                    // && DistanceTo(result) < goalSearchDistance
                    && PathfindingGoal.TryGoal(this, result, goal))
                    {
                        searchDistance = goalSearchGrowth;
                        return result;
                    }
            }
        }

        foreach (PathfindingGoal goal in GetGoals())
        {
            currentGoal = goal;

            //  TODO: There is a cleaner way to do this

            //  Radiate out layer by layer around the actor without searching previous layers
            for (int radius = 1; radius < searchDistance; radius++)
            {
                //  Search the top/bottom rows
                for (int x = -radius; x < radius; x++)
                {
                    TryGoalAtHelper(x, radius, goal, ref current, ref result, ref currentDistance, ref nearestDistance);
                    if (result == null) TryGoalAtHelper(x, -radius, goal, ref current, ref result, ref currentDistance, ref nearestDistance);

                    //  Return the first match if goals are being tested in order of priority
                    if (usePriority && result != null)
                    {
                        searchDistance = goalSearchGrowth;  //  Reset search distance
                        return result;
                    }
                }

                //  Search the side columns
                for (int y = -radius; y < radius; y++)
                {
                    TryGoalAtHelper(radius, y, goal, ref current, ref result, ref currentDistance, ref nearestDistance);
                    if (result == null) TryGoalAtHelper(-radius, y, goal, ref current, ref result, ref currentDistance, ref nearestDistance);

                    //  Return the first match if goals are being tested in order of priority
                    if (usePriority && result != null)
                    {
                        searchDistance = goalSearchGrowth;  //  Reset search distance
                        return result;
                    }
                }
            }
        }

        //  No matching goal found
        if (useBehavior && result == null)
            WipeAI();

        //  Expand the search
        if (useBehavior)
            currentGoalSearchDistance = (byte)Mathf.Clamp(searchDistance + goalSearchGrowth, 1, goalSearchDistance);

        return result;
    }

    public bool GotoNearestGoalWithPriority(bool useBehavior = true) { return GotoNearestGoal(true, useBehavior); }
    public bool GotoNearestGoal(bool usePriority = false, bool useBehavior = true)
    {
        if (isPathLocked) return false;

        if (!HasValidTarget())
        {
            currentGoalCell = FindNearestGoal(usePriority, useBehavior);
        }

        if (HasValidTarget())
        {
            Goto(currentGoalCell.x, currentGoalCell.y);
            return true;
        }

        return false;
    }

    public bool TrySetGoal(Cell cell)
    {
        PathfindingGoal goal = PathfindingGoal.GetGoal(this, cell, GetGoals());

        // if (goal != null && !goal.active)
        //     goal.active = true;

        WipeAI();

        currentGoalCell = cell;
        currentGoal = goal;

        previousGoalCell = currentGoalCell;
        previousGoal = currentGoal;

        if (PathfindingGoal.TryGoal(this, cell, goal))
        {
            GotoForced(cell.x, cell.y);
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
        if (!isPathLocked && !HasValidPath() && DistanceTo(x, y) > maxGoalInteractRange)
            PathManager.RequestPath(this, x, y, ignoreActors);
    }

    public void GotoForced(Direction dir, int distance, bool ignoreActors = true) { Goto(dir.toVector3() * distance, ignoreActors); }
    public void GotoForced(Coord2D coord, bool ignoreActors = true) { Goto(coord.x, coord.y, ignoreActors); }
    public void GotoForced(Vector2 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.y, ignoreActors); }
    public void GotoForced(Vector3 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.z, ignoreActors); }
    public void GotoForced(int x, int y, bool ignoreActors = true)
    {
        // ! Not sure if maxGoalInteractRange is needed here. Not sure if I added
        // ! it when making the changes for ranged units or you (chase) added it.
        // ! It does prevent going to a postion for ranged units if that position
        // ! is not outside of their range though, and that's a problem.
        if (!isPathLocked && DistanceTo(x, y) > 1)// maxGoalInteractRange)
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

            if (previousGoal != currentGoal)
            {
                PathfindingGoal.TriggerGoalChanged(this, previousGoal, currentGoal);
                TryDiscoverGoal(previousGoal, previousGoalCell);
            }

            previousGoalCell = currentGoalCell;
            previousGoal = currentGoal;

            //  Handle interacting with goals
            if ( HasValidTarget() && (!moving || DistanceTo(currentGoalCell) <= maxGoalInteractRange) )
            {
                //  Check if we have reached our target
                if (DistanceTo(currentGoalCell) <= maxGoalInteractRange)
                {
                    //  Assume our currentGoal is a valid match since it was found successfully.
                    //  Forcibly trigger reached under that assumption
                    PathfindingGoal.TriggerInteractGoal(this, currentGoalCell, currentGoal);

                    //  ! HasValidTarget checks the cell isn't null, so how is this resolving as null?
                    if (currentGoalCell != null)
                        LookAt(currentGoalCell.x, currentGoalCell.y);

                    ResetPathingBrain();
                    ResetPath();
                }
                // or the path ahead matches our goal
                else if (HasValidPath() && PathfindingGoal.CheckGoal(this, currentPath[0], currentGoal))
                {
                    //  Assume our currentGoal is a valid match since it was found successfully.
                    //  Forcibly trigger reached under that assumption
                    PathfindingGoal.TriggerInteractGoal(this, currentPath[0], currentGoal);

                    LookAt(currentPath[0].x, currentPath[0].y);

                    ResetPathingBrain();
                    ResetPath();
                }
            }

            //  Wipe AI state if we are idling
            if (UpdateIdle()) WipeAI();

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

            //  Try performing a micro goal search at this point before moving forward
            if (doMicroSearching)
                GotoNearestGoalWithPriority(false);

            //  Attempt to move to the next point
            if (CanSetPosition(currentPath[0].x, currentPath[0].y, canPassThruActors) )
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

                    //  Make certain the path is unlocked once the end is reached
                    if (currentPath.Count <= 0) UnlockPath();
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
                            Coord2D coord = currentGoalCell.GetFirstOccupant().GetNearbyCoord();
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
                        //  Reset pathing and memory
                        WipeAI();

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

        if (previousGoalCell != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(World.ToTransformSpace(previousGoalCell.x, previousGoalCell.y), 0.25f * World.GetUnit());
        }

        if (currentGoalCell != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(World.ToTransformSpace(currentGoalCell.x, currentGoalCell.y), 0.25f * World.GetUnit());

            if (currentGoalCell.GetFirstOccupant() != null)
            {
                Coord2D coord = currentGoalCell.GetFirstOccupant().GetDirectionalCoord(gridPosition);

                Gizmos.color = Color.green;
                Gizmos.DrawSphere(World.ToTransformSpace(coord.x, coord.y), 0.25f * World.GetUnit());
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