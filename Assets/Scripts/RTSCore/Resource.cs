using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public class Resource : Obstacle
{
    public ResourceGatheringType type = ResourceGatheringType.None;
    public int amount = 1000;

    public int GetRemoveAmount(int count)
    {
        int value = amount - count;
        int overflow = value < 0 ? Mathf.Abs(value) : 0;

        return count - overflow;
    }

    //  Removes count and returns how much was removed
    public int TryRemove(int count)
    {
        amount -= count;

        int overflow = amount < 0 ? Mathf.Abs(amount) : 0;

        if (amount <= 0)
        {
            UnbakeFromGrid();
            Destroy(this.gameObject);
        }

        return count - overflow;
    }
}
