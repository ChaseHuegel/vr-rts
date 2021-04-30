using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{

public class Actor : Body
{
    [SerializeField] protected float movementInterpolation = 1f;

    public List<Cell> currentPath = null;
    private byte pathWaitTries = 0;
    private byte pathRepathTries = 0;

    private byte pathTimer = 0;
    private byte tickTimer = 0;
    private bool frozen = false;

    public bool HasValidPath() { return (currentPath != null && currentPath.Count > 0); }

    public void Freeze() { frozen = true; RemoveFromGrid(); }
    public void Unfreeze() { frozen = false; UpdatePosition(); }
    public void ToggleFreeze()
    {
        if (frozen = !frozen == false) UpdatePosition();
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

    //  Pathfind to a position
    public void Goto(Direction dir, int distance, bool ignoreActors = true) { Goto(dir.toVector3() * distance, ignoreActors); }
    public void Goto(Coord2D coord, bool ignoreActors = true) { Goto(coord.x, coord.y, ignoreActors); }
    public void Goto(Vector2 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.y, ignoreActors); }
    public void Goto(Vector3 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.z, ignoreActors); }
    public void Goto(int x, int y, bool ignoreActors = true)
    {
        if (!HasValidPath()) PathManager.RequestPath(this, x, y, ignoreActors);
    }

    public void GotoForced(Direction dir, int distance, bool ignoreActors = true) { Goto(dir.toVector3() * distance, ignoreActors); }
    public void GotoForced(Vector2 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.y, ignoreActors); }
    public void GotoForced(Vector3 vec, bool ignoreActors = true) { Goto((int)vec.x, (int)vec.z, ignoreActors); }
    public void GotoForced(int x, int y, bool ignoreActors = true)
    {
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

        pathTimer++;
        if (pathTimer >= Constants.ACTOR_PATH_RATE)
        {
            pathTimer = 0;

            //  If we have a valid path, move along it
            if (HasValidPath() && !frozen)
            {
                LookAt(currentPath[0].x, currentPath[0].y );

                // Need to be watching the actors position in world space and update it's
                // grid position when it reaches a grid boundary so we can control the
                // movement speed of the actor properly.
                // Right now, you can turn the interpolation really low and the actor will
                // reach it's destination and start gathering in grid space while still
                // several grid spaces away from the target node in world space.

                //  We can pass thru actors if the path ahead is clear
                bool canPassThruActors = currentPath.Count > 2 ? !World.at(currentPath[1].x, currentPath[1].y).IsBlocked() : false;

                //  Attempt to move to the next point
                if (SetPosition( currentPath[0].x, currentPath[0].y, canPassThruActors ))
                {
                    currentPath.RemoveAt(0);

                    ResetPathingBrain();
                }
                //  Unable to reach the next point
                else
                {
                    pathWaitTries++;

                    // Wait some time to see if path clears
                    if (pathWaitTries > Constants.PATH_WAIT_TRIES)
                    {
                        //  Path isn't clearing, try repathing
                        if (pathRepathTries < Constants.PATH_REPATH_TRIES)
                            GotoForced(
                                currentPath[currentPath.Count - 1].x + Random.Range(-1, 1),
                                currentPath[currentPath.Count - 1].y + Random.Range(-1, 1),
                                false    //  false, dont ignore actors. Stuck and may need to path around them
                                );
                        //  Give up after repathing a number of times
                        else
                        {
                            ResetPathing();
                        }

                        pathRepathTries++;
                    }
                }

                //  Don't hang onto an empty path. Save a little memory
                if (currentPath != null && currentPath.Count == 0)
                    ResetPathing();
            }
        }

        //  Interpolate movement as long as we're not frozen
        if (!frozen && transform.position.x != gridPosition.x && transform.position.z != gridPosition.y)
        {
            transform.position = Vector3.Lerp(transform.position, World.ToTransformSpace(new Vector3(gridPosition.x, transform.position.y, gridPosition.y)), Time.fixedDeltaTime * movementInterpolation);
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