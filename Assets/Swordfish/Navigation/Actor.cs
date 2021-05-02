using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{

public class Actor : Body
{
    protected GoalHolder goals = new GoalHolder();
    public virtual PathfindingGoal[] GetGoals() { return goals.entries; }

    [Header("Actor")]
    public byte goalSearchDistance = 20;
    public float movementSpeed = 1f;

    private float movementInterpolation;
    private bool moving = false;

    public List<Cell> currentPath = null;
    private byte pathWaitTries = 0;
    private byte pathRepathTries = 0;
    private bool frozen = false;

    private byte pathTimer = 0;
    private byte tickTimer = 0;

    public bool HasValidPath() { return (currentPath != null && currentPath.Count > 0); }
    public bool IsMoving() { return moving; }

    public void Freeze() { frozen = true; RemoveFromGrid(); }
    public void Unfreeze() { frozen = false; UpdatePosition(); }
    public void ToggleFreeze()
    {
        if (frozen = !frozen == false) UpdatePosition();
    }

    public override void Initialize()
    {
        base.Initialize();

        movementInterpolation = 1f - (Constants.ACTOR_PATH_RATE / 60f);
    }


#region immutable methods

    public void UpdatePosition()
    {
        HardSnapToGrid();
        ResetPathing();
    }

    public void ResetPathingBrain()
    {
        pathWaitTries = 0;
        pathRepathTries = 0;
    }

    public void ResetPathing()
    {
        currentPath = null;
        ResetPathingBrain();
    }

    public Cell FindNearestGoalWithPriority()
    {
        Cell result = null;
        Cell current = null;

        int currentDistance = 0;
        int nearestDistance = int.MaxValue;

        foreach (PathfindingGoal goal in GetGoals())
        {
            for (int x = -goalSearchDistance; x < goalSearchDistance; x++)
            for (int y = -goalSearchDistance; y < goalSearchDistance; y++)
            {
                current = World.at(gridPosition.x + x, gridPosition.y + y);
                currentDistance = DistanceTo(current);

                if (PathfindingGoal.TryGoal(this, current, goal) && currentDistance < nearestDistance)
                {
                    nearestDistance = currentDistance;
                    result = current;
                }
            }

            if (result != null)
                return result;
        }

        return result;
    }

    public Cell FindNearestGoal()
    {
        Cell result = null;
        Cell current = null;

        int currentDistance = 0;
        int nearestDistance = int.MaxValue;

        for (int x = -goalSearchDistance; x < goalSearchDistance; x++)
        for (int y = -goalSearchDistance; y < goalSearchDistance; y++)
        {
            current = World.at(gridPosition.x + x, gridPosition.y + y);
            currentDistance = DistanceTo(current);

            if (PathfindingGoal.TryGoal(this, current, GetGoals()) && currentDistance < nearestDistance)
            {
                nearestDistance = currentDistance;
                result = current;
            }
        }

        return result;
    }

    public bool GotoNearestGoalWithPriority()
    {
        Cell target = FindNearestGoalWithPriority();
        if (target == null)
            return false;

        if (DistanceTo(target) <= 1)
            PathfindingGoal.ReachIfGoal(this, target, GetGoals());

        Goto(target.x, target.y);

        return true;
    }

    public bool GotoNearestGoal()
    {
        Cell target = FindNearestGoal();
        if (target == null)
            return false;

        if (DistanceTo(target) <= 1)
            PathfindingGoal.ReachIfGoal(this, target, GetGoals());

        Goto(target.x, target.y);

        return true;
    }

    public void Goto(Direction dir, int distance, bool ignoreActors = true) { Goto(dir.toVector3() * distance, ignoreActors); }
    public void Goto(Coord2D coord, bool ignoreActors = true) { Goto(coord.x, coord.y, ignoreActors); }
    public void Goto(Vector2 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.y, ignoreActors); }
    public void Goto(Vector3 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.z, ignoreActors); }
    public void Goto(int x, int y, bool ignoreActors = true)
    {
        if (!HasValidPath() && DistanceTo(x, y) > 1)
            PathManager.RequestPath(this, x, y, ignoreActors);
    }

    public void GotoForced(Direction dir, int distance, bool ignoreActors = true) { Goto(dir.toVector3() * distance, ignoreActors); }
    public void GotoForced(Vector2 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.y, ignoreActors); }
    public void GotoForced(Vector3 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.z, ignoreActors); }
    public void GotoForced(int x, int y, bool ignoreActors = true)
    {
        if (DistanceTo(x, y) > 1)
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
        tickTimer++;
        if (tickTimer >= Constants.ACTOR_TICK_RATE)
        {
            tickTimer = 0;
            Tick();
        }

        //  Don't pathfind while frozen
        if (frozen) return;

        Vector3 gridTransformPos = World.ToTransformSpace(gridPosition.x, transform.position.y, gridPosition.y);
        bool reachedTarget = true;

        moving = false;

        //  Interpolate movement
        if (transform.position != gridTransformPos)
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
            pathTimer = 0;  //  Path tick

        //  If we have a valid path, move along it
        if (HasValidPath())
        {
            // Need to be watching the actors position in world space and update it's
            // grid position when it reaches a grid boundary so we can control the
            // movement speed of the actor properly.
            // Right now, you can turn the interpolation really low and the actor will
            // reach it's destination and start gathering in grid space while still
            // several grid spaces away from the target node in world space.

            //  We can pass thru actors if the path ahead is clear
            bool canPassThruActors = currentPath.Count > 2 ? !World.at(currentPath[1].x, currentPath[1].y).IsBlocked() : false;

            //  Handle reaching a goal, stop pathing if we reached a goal
            if ( PathfindingGoal.ReachIfGoal(this, currentPath[0], GetGoals()) )
            {
                ResetPathing();
                return;
            }

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
                    //  Path hasn't cleared, try repathing
                    if (pathRepathTries < Constants.PATH_REPATH_TRIES)
                    {
                        GotoForced(
                            currentPath[currentPath.Count - 1].x + Random.Range(-1, 1),
                            currentPath[currentPath.Count - 1].y + Random.Range(-1, 1),
                            false    //  false, dont ignore actors. Stuck and may need to path around them
                            );
                    }
                    //  Unable to repath, resort to giving up
                    else
                    {
                        ResetPathing();
                    }

                    pathRepathTries++;
                }

                pathWaitTries++;
            }

            //  Don't hang onto an empty path. Save a little memory
            if (currentPath != null && currentPath.Count == 0)
                ResetPathing();
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
}

}