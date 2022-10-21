using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{
    public interface IActor : IBody
    {
        List<Cell> CurrentPath { get; set; }

        Cell GetCell();
    }
}
