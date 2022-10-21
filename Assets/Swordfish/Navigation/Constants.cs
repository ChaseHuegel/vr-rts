using System;

namespace Swordfish.Navigation
{

    public class Constants
    {
        public const int TICK_RATE = 20;
        public const float TICK_RATE_DELTA = 1f / TICK_RATE;
        public const int ACTOR_PATH_RATE = 10;

        public const int PATH_HEAP_SIZE = 17000;
        public const int PATH_WAIT_TRIES = 6;
        public const int PATH_REPATH_TRIES = 3;
    }

}