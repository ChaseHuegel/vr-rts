using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Swordfish.Navigation
{
    public abstract class Body : Damageable
    {
        public static List<Body> AllBodies { get; } = new List<Body>();

        public Faction Faction;

        [Header("Body Settings")]
        [SerializeField]
        protected Vector2 BoundingDimensions = Vector2.one;

        [SerializeField]
        protected Vector2 BoundingOrigin = Vector2.zero;

        [Header("Skin Settings")]
        [SerializeField]
        protected Renderer[] SkinRendererTargets = new Renderer[0];

        protected Coord2D GridPosition = new(0, 0);

        public virtual void Initialize()
        {
            AllBodies.Add(this);
        }

        protected virtual void AttachListeners() { }
        protected virtual void CleanupListeners() { }

        public virtual void Tick(float deltaTime) { }

        protected override void Start()
        {
            base.Start();
            SyncToTransform();
            Initialize();
            AttachListeners();
            UpdateSkin();
        }

        protected virtual void OnDestroy()
        {
            AllBodies.Remove(this);

            if (Application.isPlaying && gameObject.scene.isLoaded)
            {
                CleanupListeners();
                RemoveFromGrid();
            }
        }

        protected virtual void OnValidate()
        {
            if (!GameMaster.Instance)
                return;

            UpdateSkin();
        }

        /// <summary>
        ///     Gets the grid position of this <see cref="Body"/>.
        /// </summary>
        public Coord2D GetPosition()
        {
            return GridPosition;
        }

        /// <summary>
        ///     Gets the bounding dimensions of this <see cref="Body"/>.
        /// </summary>
        public Vector2 GetBoundingDimensions()
        {
            return BoundingDimensions;
        }

        /// <summary>
        ///     Gets the bounding origin of this <see cref="Body"/>.
        /// </summary>
        public Vector2 GetBoundingOrigin()
        {
            return BoundingOrigin;
        }

        /// <summary>
        ///     Get the cell of this <see cref="Body"/>.
        /// </summary>
        public Cell GetCell()
        {
            return World.Grid.at(GridPosition.x, GridPosition.y);
        }

        /// <summary>
        ///     Get the cell located at the transform.
        /// </summary>
        public Cell GetTransformCell()
        {
            Vector3 pos = World.ToWorldSpace(transform.position);
            return World.Grid.at((int)pos.x, (int)pos.z);
        }

        /// <summary>
        ///     Gets the nearest position relative to a provided
        ///     position that is occupied by this <see cref="Body"/>.
        ///     This method should be used instead of <see cref="GetPosition"/>
        ///     when the <see cref="BoundingDimensions"/> should be considered.
        /// </summary>
        /// <param name="from">The position to get the nearest to.</param>
        /// <returns>The nearest position.</returns>
        public Coord2D GetNearestPositionFrom(Coord2D from)
        {
            int shortestDistance = int.MaxValue;
            Coord2D nearestPosition = new(0, 0);

            for (int x = -(int)(BoundingDimensions.x / 2); x < BoundingDimensions.x / 2; x++)
            {
                for (int y = -(int)(BoundingDimensions.y / 2); y < BoundingDimensions.y / 2; y++)
                {
                    var position = GridPosition + new Coord2D(x, y);
                    var distance = position.GetDistanceTo(from.x, from.y);

                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        nearestPosition = position;
                    }
                }
            }

            return nearestPosition;
        }

        /// <summary>
        ///     Gets a random position adjacent to this <see cref="Body"/>.
        ///     This considers the <see cref="BoundingDimensions"/>.
        /// </summary>
        /// <returns></returns>
        public Coord2D GetRandomAdjacentPosition()
        {
            Coord2D target = new(GridPosition.x, GridPosition.y);
            int paddingX = Random.Range(1, (int)(BoundingDimensions.x * 0.5f) + 2);
            int paddingY = Random.Range(1, (int)(BoundingDimensions.y * 0.5f) + 2);

            //  Use 0 and 1 to determine negative or positive
            int sign = Random.value > 0.5f ? -1 : 1;
            target.x += sign * paddingX;

            sign = Random.value > 0.5f ? -1 : 1;
            target.y += sign * paddingY;

            return target;
        }

        /// <summary>
        ///     Get the distance to a position from this <see cref="Body"/>.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <returns>The distance to the position in grid cells.</returns>
        public int GetDistanceTo(int x, int y)
        {
            int distX = Mathf.Abs(x - GridPosition.x);
            int distY = Mathf.Abs(y - GridPosition.y);

            return distX > distY ? distX : distY;
        }

        /// <summary>
        ///     Checks whether the provided grid position is valid to move to.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="ignoreOccupied">Whether to ignore the position's occupied state.</param>
        /// <returns>True if the movement is valid; otherwise false.</returns>
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

        /// <summary>
        ///     Attempt to move the grid position relative to the current.
        ///     This validates the new position but not the path between.
        /// </summary>
        /// <param name="xAmount">The amount to move the x position.</param>
        /// <param name="yAmount">The amount to move the y position.</param>
        /// <param name="ignoreOccupied">Whether to ignore the position's occupied state.</param>
        /// <returns>True if the movement was successful; otherwise false.</returns>
        public bool TryMove(int xAmount, int yAmount, bool ignoreOccupied = false)
        {
            return TrySetPosition(GridPosition.x + xAmount, GridPosition.y + yAmount, ignoreOccupied);
        }

        /// <summary>
        ///     Attempt to set the grid position if the new position is valid.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <param name="ignoreOccupied">Whether to ignore the position's occupied state.</param>
        /// <returns>True if the movement was successful; otherwise false.</returns>
        public bool TrySetPosition(int x, int y, bool ignoreOccupied = false)
        {
            Cell to = World.at(x, y);

            //  Only move if cell passable, and not occupied (if we arent ignoring occupied cells)
            if (to.passable)
            {
                if (to.occupied && !ignoreOccupied)
                    return false;

                Cell from = GetCell();

                from.occupants.Remove(this);
                to.occupants.Add(this);

                GridPosition.x = x;
                GridPosition.y = y;

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Set the grid position without performing any validation.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        public void SetPosition(int x, int y)
        {
            Cell to = World.at(x, y);
            Cell from = GetCell();

            from.occupants.Remove(this);
            to.occupants.Add(this);

            GridPosition.x = x;
            GridPosition.y = y;
        }

        /// <summary>
        ///     Remove from the grid.
        /// </summary>
        public void RemoveFromGrid()
        {
            Cell cell = World.at(GridPosition);
            cell.passable = true;
            cell.canPathThru = false;
            cell.occupants.Remove(this);
        }

        /// <summary>
        ///     Updates the grid position to match the transform.
        /// </summary>
        protected virtual void SyncToTransform()
        {
            Coord2D worldPos = World.ToWorldCoord(transform.position);

            Cell to = World.at(worldPos.x, worldPos.y);
            Cell from = GetCell();

            from.occupants.Remove(this);
            to.occupants.Add(this);

            GridPosition.x = worldPos.x;
            GridPosition.y = worldPos.y;
        }

        /// <summary>
        ///     Updates the transform to match the grid position.
        /// </summary>
        public virtual void SyncToGrid()
        {
            transform.position = World.ToTransformSpace(new Vector3(GridPosition.x, transform.position.y, GridPosition.y));

            //  If origin has been set, use it. Otherwise, calculate it.
            if (BoundingOrigin != Vector2.zero)
                transform.position += new Vector3(BoundingOrigin.x, 0f, BoundingOrigin.y);
            else
            {
                Vector3 modPos = transform.position;

                if (BoundingDimensions.x % 2 == 0)
                    modPos.x = transform.position.x + World.GetUnit() * -0.5f;

                if (BoundingDimensions.y % 2 == 0)
                    modPos.z = transform.position.z + World.GetUnit() * -0.5f;

                transform.position = modPos;
            }
        }

        private void UpdateSkin()
        {
            if (SkinRendererTargets.Length <= 0) return;
            
            if (Faction?.skin?.unitMaterial)
            {
                foreach (var renderer in SkinRendererTargets)
                    renderer.sharedMaterial = Faction.skin.unitMaterial;
            }
        }
    }

}