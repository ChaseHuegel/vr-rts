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
        if (goal != null && goal.active && goal.CheckGoal(cell))
            return true;

        return false;
    }

    //  Try a set of goals
    public static bool TryGoal(Actor actor, Cell cell, PathfindingGoal[] goals)
    {
        if (goals == null) return false;

        foreach (PathfindingGoal goal in goals)
            if (TryGoal(actor, cell, goal))
                return true;

        return false;
    }

    //  Try a single goal
    public static bool TryGoal(Actor actor, Cell cell, PathfindingGoal goal)
    {
        if (goal != null && CheckGoal(actor, cell, goal))
        {
            goal.OnFoundGoal(actor, cell);
            return true;
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

    //  Trigger a reached event forcefully without checking if its a valid match
    public static bool TriggerReachedGoal(Actor actor, Cell cell, PathfindingGoal goal)
    {
        if (goal != null && goal.active)
        {
            goal.OnReachedGoal(actor, cell);
            return true;
        }

        return false;
    }

    public bool active = true;

    public virtual bool CheckGoal(Cell cell) { return false; }

    //  TODO: Turn these into actual events
    public virtual void OnFoundGoal(Actor actor, Cell cell) {}
    public virtual void OnReachedGoal(Actor actor, Cell cell) {}
}

}