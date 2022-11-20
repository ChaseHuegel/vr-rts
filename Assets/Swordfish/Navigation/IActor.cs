using System;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Navigation
{
    public interface IActor
    {
        NavigationLayers Layers { get; set; }

        List<Cell> CurrentPath { get; set; }

        Cell GetCell();
    }
}
