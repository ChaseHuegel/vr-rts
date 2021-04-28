using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{

public class Body : MonoBehaviour
{
    public Coord2D gridPosition = new Coord2D(0, 0);

    public virtual void Initialize() {}
    public virtual void Tick() {}

    private void Start()
    {
        //  Snap to the grid with rounding
        //  Round instead of truncate the initial position (i.e. obstacles which don't align with the grid)
        //  Rounding is more accurate but has more overhead than truncating
        //  Do NOT round unless necessary! Movement is bound to the grid, accuracy not an issue
        HardSnapToGrid();

        Initialize();
    }


#region getters setters

    public Cell GetCellAtTransform()
    {
        Vector3 pos = World.ToWorldSpace(transform.position);
        return World.Grid.at((int)pos.x, (int)pos.z);
    }

    public Cell GetCellAtGrid()
    {
        return World.Grid.at(gridPosition.x, gridPosition.y);
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

            from.occupied = false;
            to.occupied = true;

            gridPosition.x = x;
            gridPosition.y = y;

            return true;    //  We were able to move
        }

        return false;   // We were unable to move
    }

    //  Force to a spot in the grid regardless of what else is there
    public void SetPositionUnsafe(int x, int y)
    {
        Cell to = World.at(x, y);
        Cell from = GetCellAtGrid();

        from.occupied = false;
        to.occupied = true;

        gridPosition.x = x;
        gridPosition.y = y;
    }

    //  Remove this body from the grid
    public void RemoveFromGrid()
    {
        Cell cell = World.at(gridPosition);
        cell.occupied = false;
        cell.passable = true;
    }

    //  Perform a 'soft' snap by truncating. Inaccurate but less overhead.
    public void SnapToGrid()
    {
        Vector3 pos = World.ToWorldSpace(transform.position);
        gridPosition.x = (int)pos.x;
        gridPosition.y = (int)pos.z;

        UpdateTransform();
    }

    //  Perform a 'hard' snap by rounding. More accurate with more overhead.
    public void HardSnapToGrid()
    {
        Vector3 pos = World.ToWorldSpace(transform.position);

        gridPosition.x = Mathf.RoundToInt(pos.x);
        gridPosition.y = Mathf.RoundToInt(pos.z);

        UpdateTransform();
    }

    //  Force the transform to match the grid position
    public void UpdateTransform()
    {
        transform.position = World.ToTransformSpace(new Vector3(gridPosition.x, transform.position.y, gridPosition.y));
    }
#endregion
}

}