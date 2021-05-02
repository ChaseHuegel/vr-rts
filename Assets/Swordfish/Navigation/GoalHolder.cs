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

    public void Add<T>() where T : PathfindingGoal
    {
        goals.Add( (T)System.Activator.CreateInstance(typeof(T)) );
    }

    public void Remove<T>() where T : PathfindingGoal
    {
        goals.Remove( (T)System.Activator.CreateInstance(typeof(T))  );
    }

    public T Get<T>() where T : PathfindingGoal
    {
        return (T)goals.Find(x => x is T);
    }
}

}