using System;
using System.Collections.Generic;

namespace Swordfish.Navigation
{

public class GoalHolder
{
    private List<PathfindingGoal> goals = new List<PathfindingGoal>();
    public PathfindingGoal[] entries
    {
        get { return goals.ToArray(); }
    }

    public T Add<T>() where T : PathfindingGoal
    {
        T goal = (T)System.Activator.CreateInstance(typeof(T));
        goals.Add(goal);
        return goal;
    }

    public void Remove<T>() where T : PathfindingGoal
    {
        goals.Remove( (T)System.Activator.CreateInstance(typeof(T))  );
    }

    public T Get<T>() where T : PathfindingGoal
    {
        return (T)goals.Find(x => x is T);
    }

    public T Get<T>(Predicate<PathfindingGoal> expression) where T : PathfindingGoal
    {
        return (T)goals.Find( expression );
    }
}

}