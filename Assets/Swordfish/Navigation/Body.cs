using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{

public class Body : MonoBehaviour
{
    [Header("Body")]
    public Vector2 boundingDimensions = Vector2.one;
    public Vector2 boundingOrigin = Vector2.zero;
    public Coord2D gridPosition = new Coord2D(0, 0);

    public byte factionId;
    
    public bool IsSameFaction(Actor actor)
    {
        return this.factionId == actor.factionId;
    }

    public bool IsSameFaction(byte factionId)
    {
        return this.factionId == factionId;
    }

    public virtual void Initialize() {}
    public virtual void Tick() {}

    private void Start()
    {
        SyncPosition();
        Initialize();
    }

    // public virtual void OnDrawGizmos()
    // {
    //     if (Application.isEditor != true || Application.isPlaying) return;

    //     Vector3 pos = transform.position;
    //     pos.x += boundingOrigin.x;
    //     pos.z += boundingOrigin.y;

    //     Vector3 worldPos = World.ToWorldSpace(pos);
    //     Vector3 gridPoint = World.ToTransformSpace( worldPos);

    //     Gizmos.matrix = Matrix4x4.TRS(gridPoint - World.GetUnitOffset(), Quaternion.identity, Vector3.one * GetBoundsVolume());
    //     Gizmos.color = Color.cyan;

    //     Gizmos.DrawLine( new Vector3(0, 0, 0), new Vector3(0, 0, World.GetUnit()) );
    //     Gizmos.DrawLine( new Vector3(0, 0, World.GetUnit()), new Vector3(World.GetUnit(), 0,World.GetUnit()));
    //     Gizmos.DrawLine( new Vector3(World.GetUnit(), 0, World.GetUnit()), new Vector3(World.GetUnit(), 0, 0) );
    //     Gizmos.DrawLine( new Vector3(World.GetUnit(), 0, 0), new Vector3(0, 0, 0) );
    // }


#region getters setters

    /// <summary>
    /// Get the grid cell located at the transform of this body.
    /// </summary>
    /// <returns>Cell - The grid cell the body is located in.</returns>
    public Cell GetCellAtTransform()
    {
        Vector3 pos = World.ToWorldSpace(transform.position);
        return World.Grid.at((int)pos.x, (int)pos.z);
    }

    public Cell GetCellAtGrid()
    {
        return World.Grid.at(gridPosition.x, gridPosition.y);
    }

    public Coord2D GetNearbyCoord()
    {
        Coord2D target = new Coord2D(gridPosition.x, gridPosition.y);
        int paddingX = Random.Range(1, (int)(boundingDimensions.x * 0.5f) + 2);
        int paddingY = Random.Range(1, (int)(boundingDimensions.y * 0.5f) + 2);

        //  Use 0 and 1 to determine negative or positive
        int sign = Random.value > 0.5f ? -1 : 1;
        target.x += sign * paddingX;

        sign = Random.value > 0.5f ? -1 : 1;
        target.y += sign * paddingY;

        return target;
    }

    //  Used in transform space
    public float GetBoundsVolumeSqr() { return GetBoundsVolume() * GetBoundsVolume(); }
    public float GetBoundsVolume()
    {
        return (boundingDimensions.x + boundingDimensions.y) * 0.5f + World.GetUnit();
    }

    //  Used in world space
    public float GetCellVolumeSqr() { return GetCellVolume() * GetCellVolume(); }
    public float GetCellVolume()
    {
        return (boundingDimensions.x + boundingDimensions.y);
    }

    public int DistanceTo(Cell cell) { return DistanceTo(cell.x, cell.y); }
    public int DistanceTo(Coord2D coord) { return DistanceTo(coord.x, coord.y); }
    public int DistanceTo(int x, int y)
    {
        int distX = Mathf.Abs(x - gridPosition.x);
        int distY = Mathf.Abs(y - gridPosition.y);

        return distX > distY ? distX : distY;
    }

    public bool CanSetPosition(Vector2 p, bool ignoreOccupied = false) { return CanSetPosition((int)p.x, (int)p.y, ignoreOccupied); }
    public bool CanSetPosition(Vector3 p, bool ignoreOccupied = false) { return CanSetPosition((int)p.x, (int)p.z, ignoreOccupied); }
    public bool CanSetPosition(int x, int y, bool ignoreOccupied = false)
    {
        Cell to = World.at(x, y);

        if (to.passable)
        {
            if (to.occupied && !ignoreOccupied)
                return false;

            return true;
        }

        return false;
    }
#endregion


#region immutable methods

    //  Move relative to current position
    public bool Move(Direction dir, bool ignoreOccupied = false) { return Move(dir.toVector3(), ignoreOccupied); }
    public bool Move(Vector2 vec, bool ignoreOccupied = false) { return Move((int)vec.x, (int)vec.y, ignoreOccupied); }
    public bool Move(Vector3 vec, bool ignoreOccupied = false) { return Move((int)vec.x, (int)vec.z, ignoreOccupied); }
    public bool Move(int x, int y, bool ignoreOccupied = false)
    {
        return SetPosition( gridPosition.x + x, gridPosition.y + y );
    }

    //  Set position snapped to the grid
    public bool SetPosition(Vector2 p, bool ignoreOccupied = false) { return SetPosition((int)p.x, (int)p.y, ignoreOccupied); }
    public bool SetPosition(Vector3 p, bool ignoreOccupied = false) { return SetPosition((int)p.x, (int)p.z, ignoreOccupied); }
    public bool SetPosition(int x, int y, bool ignoreOccupied = false)
    {
        Cell to = World.at(x, y);

        //  Only move if cell passable, and not occupied (if we arent ignoring occupied cells)
        if (to.passable)
        {
            if (to.occupied && !ignoreOccupied)
                return false;

            Cell from = GetCellAtGrid();

            from.occupants.Remove(this);
            to.occupants.Add(this);

            gridPosition.x = x;
            gridPosition.y = y;

            return true;    //  We were able to move
        }

        return false;   // We were unable to move
    }

    //  Force to a spot in the grid regardless of what else is there
    public void SetPositionUnsafe(Coord2D coord) { SetPositionUnsafe(coord.x, coord.y); }
    public void SetPositionUnsafe(Coord3D coord) { SetPositionUnsafe(coord.x, coord.z); }
    public void SetPositionUnsafe(Vector3 pos) { SetPositionUnsafe((int)pos.x, (int)pos.z); }
    public void SetPositionUnsafe(int x, int y)
    {
        Cell to = World.at(x, y);
        Cell from = GetCellAtGrid();

        from.occupants.Remove(this);
        to.occupants.Add(this);

        gridPosition.x = x;
        gridPosition.y = y;
    }

    //  Update grid position to match transform position
    //  This is similar to removing from grid, moving, and snapping to grid
    //  Use this to fix desync between the grid and transform pos
    public void SyncPosition()
    {
        Coord2D worldPos = World.ToWorldCoord(transform.position);

        Cell to = World.at(worldPos.x, worldPos.y);
        Cell from = GetCellAtGrid();

        from.occupants.Remove(this);
        to.occupants.Add(this);

        gridPosition.x = worldPos.x;
        gridPosition.y = worldPos.y;
    }

    //  Remove this body from the grid
    public void RemoveFromGrid()
    {
        Cell cell = World.at(gridPosition);
        cell.passable = true;
        cell.occupants.Remove(this);
    }

    //  Force the transform to match the grid position
    public void UpdateTransform()
    {
        transform.position = World.ToTransformSpace(new Vector3(gridPosition.x, transform.position.y, gridPosition.y));

        //  If origin has been set, use it. Otherwise, calculate it.
        if (boundingOrigin != Vector2.zero)
            transform.position += new Vector3(boundingOrigin.x, 0f, boundingOrigin.y);
        else
        {
            Vector3 modPos = transform.position;

            if (boundingDimensions.x % 2 == 0)
                modPos.x = transform.position.x + World.GetUnit() * -0.5f;

            if (boundingDimensions.y % 2 == 0)
                modPos.z = transform.position.z + World.GetUnit() * -0.5f;

            transform.position = modPos;
        }
    }
#endregion
}

}