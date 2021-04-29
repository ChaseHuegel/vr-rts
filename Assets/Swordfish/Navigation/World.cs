﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{

public class World : Singleton<World>
{
    public bool showDebugGrid = true;

    [SerializeField] protected int gridSize = 10;
    [SerializeField] protected float gridUnit = 1;

    private Grid grid;
    public static Grid Grid { get { return Instance.grid; } }

    //  Grid info
    public static float GetUnit() { return Instance.gridUnit; }
    public static float GetScale() { return 1f/Instance.gridUnit; }

    //  World info
    public static float GetLength() { return Instance.gridSize; }
    public static float GetSize() { return Instance.gridSize * Instance.gridUnit; }

    //  Transform info
    public static Vector3 GetOrigin() { return Instance.transform.position; }
    public static Vector3 GetCenteredPosition() { return GetOrigin() + GetGridOffset(); }
    public static Vector3 GetUnitOffset() { return new Vector3(GetUnit() * 0.5f, 0f, GetUnit() * 0.5f); }
    public static Vector3 GetGridOffset() { return new Vector3(GetSize() * -0.5f, 0f, GetSize() * -0.5f); }

    //  Shorthand access to grid
    public static Cell at(Coord2D coord) { return Grid.at(coord.x, coord.y); }
    public static Cell at(int x, int y) { return Grid.at(x, y); }

    //  Convert from grid units to transform units
    public static Vector3 ToTransformSpace(Vector3 pos)
    {
        Vector3 result = (pos + GetOrigin()) * GetUnit() + GetGridOffset() + GetUnitOffset();
        result.y = pos.y;
        return result;
    }

    //  Convert from transform units to grid units
    public static Vector3 ToWorldSpace(Vector3 pos)
    {
        // Vector3 result = ((pos + World.GetOrigin()) + (Vector3.one * World.GetSize()/2)) / World.GetUnit();

        Vector3 result = pos - ((GetOrigin() - GetUnitOffset()) * GetUnit()) + (Vector3.one * World.GetSize()/2f);
        result /= World.GetUnit();

        result.x = Mathf.FloorToInt( result.x );
        result.z = Mathf.FloorToInt( result.z );
        result.y = pos.y;

        return result;
    }

    private void Start()
    {
        grid = new Grid(gridSize);
    }

    //  Debug draw the grid
    private void OnDrawGizmos()
    {
        if (Application.isEditor != true) return;

        //  Center at 0,0 on the grid
        Gizmos.matrix = Matrix4x4.TRS(GetCenteredPosition(), Quaternion.identity, Vector3.one);
        Gizmos.color = Color.yellow;

        //  Bounds
        Gizmos.DrawLine( new Vector3(0, 0, 0), new Vector3(0, 0, GetSize()) );
        Gizmos.DrawLine( new Vector3(0, 0, GetSize()), new Vector3(GetSize(), 0, GetSize()));
        Gizmos.DrawLine( new Vector3(GetSize(), 0, GetSize()), new Vector3(GetSize(), 0, 0) );
        Gizmos.DrawLine( new Vector3(GetSize(), 0, 0), new Vector3(0, 0, 0) );

        //  Center on the world origin
        Gizmos.matrix = Matrix4x4.TRS(GetOrigin(), Quaternion.identity, Vector3.one);

        //  Grid
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                //  Create a checkered pattern
                bool upper = (x % 2 == 0 && y % 2 != 0);
                bool lower = (x % 2 != 0 && y % 2 == 0);
                Gizmos.color = (upper || lower) ? Color.gray : Color.black;

                if (grid != null)
                {
                    if (!at(x, y).passable)
                        Gizmos.color = Color.yellow;
                    else if (at(x, y).occupied)
                        Gizmos.color = Color.blue;

                    if (!showDebugGrid && at(x, y).IsBlocked())
                    {
                        Gizmos.color *= new Color(1f, 1f, 1f, 0.15f);
                        Gizmos.DrawCube( ToTransformSpace( new Vector3(x, 0f, y) ), new Vector3(GetUnit(), 0f, GetUnit()));
                    }
                }

                if (showDebugGrid)
                {
                    Gizmos.color *= new Color(1f, 1f, 1f, 0.15f);
                    Gizmos.DrawCube( ToTransformSpace( new Vector3(x, 0f, y) ), new Vector3(GetUnit(), 0f, GetUnit()));
                }
            }
        }
    }
}

}