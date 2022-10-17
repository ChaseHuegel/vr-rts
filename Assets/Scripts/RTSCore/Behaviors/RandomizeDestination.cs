using System;
using Swordfish;
using Swordfish.Library.BehaviorTrees;
using Swordfish.Library.Util;
using Swordfish.Navigation;

public class RandomizeDestination : BehaviorNode<VillagerV2>
{
    public int MaxRange { get; set; }

    public RandomizeDestination(int maxRange)
    {
        MaxRange = maxRange;
    }

    public override BehaviorState Evaluate(VillagerV2 target, float delta)
    {
        Coord2D randomPos = target.gridPosition + new Coord2D(
                MathS.Random.Next(MaxRange * 2) - MaxRange,
                MathS.Random.Next(MaxRange * 2) - MaxRange
            );

        Cell cell = World.at(randomPos);

        if (cell.occupied)
            cell = World.at(cell.occupants[0].GetDirectionalCoord(target.gridPosition));

        if (cell != null)
        {
            target.Destination = cell;
            return BehaviorState.SUCCESS;
        }

        return BehaviorState.FAILED;
    }
}
