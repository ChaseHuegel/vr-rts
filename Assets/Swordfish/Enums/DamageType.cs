using System;

namespace Swordfish
{   
    [Flags]
    public enum DamageType
    {
        NONE = 0,
        BLUDGEONING = 1,
        PIERCING = 2,
        SLASHING = 4,
        HACKING = 8,
    }
}