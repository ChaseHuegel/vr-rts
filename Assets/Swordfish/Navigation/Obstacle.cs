using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{

    public class Obstacle : Body
    {
        public bool bakeOnStart = true;
        public bool allowPathThru = false;

        private bool baked;

        public override void Initialize()
        {
            base.Initialize();

            FetchBoundingDimensions();

            if (bakeOnStart)
                BakeToGrid();
        }

        protected override void OnDestroy()
        {
            if (Application.isPlaying && gameObject.scene.isLoaded)
                UnbakeFromGrid();
        }

        public virtual void FetchBoundingDimensions() { }

        public void BakeToGrid()
        {
            if (baked)
                return;

            baked = true;
                      
            Vector3 pos = World.ToWorldSpace(transform.position);
            Cell cell = World.at((int)pos.x, (int)pos.z);

            // Flag initial cell as not passable, the initial cell is by default
            // added to occupants by Body so it fails TryAdd checks and isn't able to 
            // get it's passable flag set in the loop.
            cell.passable = false;
            cell.canPathThru = allowPathThru;

            //  Block all cells within bounds  
            for (int x = -(int)(BoundingDimensions.x / 2); x < BoundingDimensions.x / 2; x++)
            {
                for (int y = -(int)(BoundingDimensions.y / 2); y < BoundingDimensions.y / 2; y++)
                {
                    cell = World.at((int)pos.x + x, (int)pos.z + y);

                    if (cell.occupants.TryAdd(this))
                    {
                        cell.passable = false;
                        cell.canPathThru = allowPathThru;
                    }
                    
                }
            }

            SyncToGrid();
        }

        public void UnbakeFromGrid()
        {
            if (!baked)
                return;

            baked = false;

            //  Unblock all cells within bounds
            Cell cell;
            for (int x = -(int)(BoundingDimensions.x / 2); x < BoundingDimensions.x / 2; x++)
            {
                for (int y = -(int)(BoundingDimensions.y / 2); y < BoundingDimensions.y / 2; y++)
                {
                    Vector3 pos = World.ToWorldSpace(transform.position);

                    cell = World.at((int)pos.x + x, (int)pos.z + y);

                    if (cell.occupants.Remove(this))
                    {
                        cell.passable = true;
                        cell.canPathThru = false;
                    }
                }
            }
        }

        public void OnDrawGizmos()
        {
            if (Application.isEditor != true || Application.isPlaying) return;

            Vector3 worldPos = World.ToWorldSpace(transform.position);

            Vector3 gridPoint = World.ToTransformSpace(worldPos);
            Gizmos.matrix = Matrix4x4.TRS(gridPoint, Quaternion.identity, Vector3.one);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(new Vector3(BoundingOrigin.x, 0f, BoundingOrigin.y), new Vector3(BoundingOrigin.x, World.GetUnit() * 1.5f, BoundingOrigin.y));

            Coord2D gridPos = new Coord2D(0, 0);
            gridPos.x = Mathf.FloorToInt(worldPos.x);
            gridPos.y = Mathf.FloorToInt(worldPos.z);

            for (int x = -(int)(BoundingDimensions.x / 2); x < BoundingDimensions.x / 2; x++)
            {
                for (int y = -(int)(BoundingDimensions.y / 2); y < BoundingDimensions.y / 2; y++)
                {
                    gridPoint = World.ToTransformSpace(new Vector3(
                        gridPos.x + x,
                        0f,
                        gridPos.y + y
                        ));

                    Gizmos.matrix = Matrix4x4.TRS(gridPoint, Quaternion.identity, Vector3.one);

                    Gizmos.color = bakeOnStart ? Color.yellow : Color.red;
                    Gizmos.DrawCube(Vector3.zero, World.GetUnit() * 0.25f * Vector3.one);
                }
            }
        }
    }

}