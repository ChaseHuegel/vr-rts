using System;
using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Library.Util;
using Swordfish.Navigation;

public class RandomizeDestination : BehaviorNode<ActorV2>
{
    public int MaxRange { get; set; }

    public RandomizeDestination(int maxRange)
    {
        MaxRange = maxRange;
    }

    public override BehaviorState Evaluate(ActorV2 actor, float delta)
    {
        Coord2D randomPos = actor.GetPosition() + new Coord2D(
                MathS.Random.Next(MaxRange * 2) - MaxRange,
                MathS.Random.Next(MaxRange * 2) - MaxRange
            );

        Cell cell = World.at(randomPos);

        if (cell.occupied)
            cell = World.at(cell.occupants[0].GetNearestPositionFrom(actor.GetPosition()));

        if (cell != null)
        {
            actor.Destination = cell;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
