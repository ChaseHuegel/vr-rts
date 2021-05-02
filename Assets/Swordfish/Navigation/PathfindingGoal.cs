namespace Swordfish.Navigation
{

public class PathfindingGoal
{
    public static bool IsGoal(Cell cell, PathfindingGoal[] goals)
    {
        if (goals == null) return false;

        foreach (PathfindingGoal goal in goals)
        {
            if (goal != null && goal.active && goal.CheckGoal(cell))
                return true;
        }

        return false;
    }

    public static bool CheckGoal(Actor actor, Cell cell, PathfindingGoal goal)
    {
        if (goal == null) return false;

        if (goal != null && goal.active && goal.CheckGoal(cell))
            return true;

        return false;
    }

    public static bool TryGoal(Actor actor, Cell cell, PathfindingGoal goal) { return TryGoal( actor, cell, new PathfindingGoal[] {goal} ); }
    public static bool TryGoal(Actor actor, Cell cell, PathfindingGoal[] goals)
    {
        if (goals == null) return false;

        foreach (PathfindingGoal goal in goals)
        {
            if (CheckGoal(actor, cell, goal))
            {
                goal.OnFoundGoal(actor, cell);
                return true;
            }
        }

        return false;
    }

    public static bool ReachIfGoal(Actor actor, Cell cell, PathfindingGoal[] goals)
    {
        if (goals == null) return false;

        foreach (PathfindingGoal goal in goals)
        {
            if (goal != null && goal.active && goal.CheckGoal(cell))
            {
                goal.OnReachedGoal(actor, cell);
                return true;
            }
        }

        return false;
    }

    public bool active = true;

    public virtual bool CheckGoal(Cell cell) { return false; }
    public virtual void OnFoundGoal(Actor actor, Cell cell) {}
    public virtual void OnReachedGoal(Actor actor, Cell cell) {}
}

}