using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{
    public interface IActor : IBody
    {
        Damageable AttributeHandler { get; }
        List<Cell> currentPath { get; set; }

        void Freeze();
        void Goto(Direction dir, int distance, bool ignoreActors = true);
        void Goto(Coord2D coord, bool ignoreActors = true);
        void Goto(Vector2 vec, bool ignoreActors = true);
        void Goto(Vector3 vec, bool ignoreActors = true);
        void Goto(int x, int y, bool ignoreActors = true);
        void GotoForced(Direction dir, int distance, bool ignoreActors = true);
        void GotoForced(Coord2D coord, bool ignoreActors = true);
        void GotoForced(Vector2 vec, bool ignoreActors = true);
        void GotoForced(Vector3 vec, bool ignoreActors = true);
        void GotoForced(int x, int y, bool ignoreActors = true);
        bool HasValidPath();
        bool IsIdle();
        bool IsMoving();
        bool IsPathLocked();
        void LockPath();
        void LookAt(float x, float y);
        void ResetPath();
        void ToggleFreeze();
        void Unfreeze();
        void UnlockPath();
        void UpdatePosition();
    }
}
