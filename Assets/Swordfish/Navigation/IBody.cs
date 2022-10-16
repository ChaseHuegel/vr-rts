using System;
using UnityEngine;

namespace Swordfish.Navigation
{
    public interface IBody
    {
        bool CanSetPosition(Vector2 p, bool ignoreOccupied = false);
        bool CanSetPosition(Vector3 p, bool ignoreOccupied = false);
        bool CanSetPosition(int x, int y, bool ignoreOccupied = false);
        int DistanceTo(Body body);
        int DistanceTo(Cell cell);
        int DistanceTo(Coord2D coord);
        int DistanceTo(int x, int y);
        float GetBoundsVolume();
        float GetBoundsVolumeSqr();
        Cell GetCellAtGrid();
        Cell GetCellAtTransform();
        Cell GetCellDirectional(Coord2D from);
        float GetCellVolume();
        float GetCellVolumeSqr();
        Coord2D GetDirectionalCoord(Coord2D from);
        Coord2D GetNearbyCoord();
        bool IsSameFaction(Actor actor);
        bool IsSameFaction(byte factionId);
        bool Move(Direction dir, bool ignoreOccupied = false);
        bool Move(Vector2 vec, bool ignoreOccupied = false);
        bool Move(Vector3 vec, bool ignoreOccupied = false);
        bool Move(int x, int y, bool ignoreOccupied = false);
        void RemoveFromGrid();
        bool SetPosition(Vector2 p, bool ignoreOccupied = false);
        bool SetPosition(Vector3 p, bool ignoreOccupied = false);
        bool SetPosition(int x, int y, bool ignoreOccupied = false);
        void SetPositionUnsafe(Coord2D coord);
        void SetPositionUnsafe(Coord3D coord);
        void SetPositionUnsafe(Vector3 pos);
        void SetPositionUnsafe(int x, int y);
        void SyncPosition();
        void Tick();
        void UpdateTransform();
    }
}
