using System;

namespace Swordfish.Navigation
{
    [Flags]
    public enum NavigationLayers
    {
        DEFAULT = 1,
        LAYER1 = 2,
        LAYER2 = 4,
        LAYER3 = 8,
        LAYER4 = 16,
        LAYER5 = 32,
        LAYER6 = 64,
        LAYER7 = 128,
        LAYER8 = 256,
        LAYER9 = 512,
        ALL = ~0
    }
}