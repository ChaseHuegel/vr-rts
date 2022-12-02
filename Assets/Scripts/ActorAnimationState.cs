[System.Serializable]
public enum ActorAnimationState
{
    IDLE = 0,
    MOVING = 1,
    FARMING = 2,
    MINING = 3,
    LUMBERJACKING = 4,
    HEAL = 5,
    ATTACKING = 6,
    ATTACKING2 = 7,
    FORAGING = 8,
    FISHING = 9,
    DYING = 10,
    DYING2 = 11,
    HUNTING = 12,
}

[System.Serializable]
public enum ActorAnimationTrigger
{
    ATTACK = 1,
    EAT = 2,
    LOOKAROUND = 3,
}


