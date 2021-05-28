using UnityEngine;
using Swordfish;
using Swordfish.Navigation;
using System.Collections.Generic;

public class Resource : Obstacle
{
    public ResourceGatheringType type = ResourceGatheringType.None;
    public float amount = 1000;

    public int maxInteractors = 0;
    //public int interactors = 0;
    public List<Unit> interactors;

    // ! Can probable remove this if the new functionality works out.
    public bool IsBusy()//Unit unit = null)
    {
        // if (interactors.Contains(unit))
        //     return false;

        return (maxInteractors > 0 && interactors.Count >= maxInteractors);
    }

    /// <summary>
    /// Returns true (can interact) if unit is already slotted at this
    /// resource or is added to the available slots. Returns false 
    /// if unit can not interact with this resource.
    /// </summary>
    /// <param name="unit">The unit requesting interaction.</param>
    /// <returns>True if unit can interact, false otherwise.</returns>
    public bool AddInteractor(Unit unit)
    {
        if (interactors.Contains(unit))
            return true;

        if (interactors.Count < maxInteractors)
        {
            interactors.Add(unit);
            return true;
        }

        return false;
    }

    public void RemoveInteractor(Unit unit)
    {
        interactors.Remove(unit);
    }

    public float GetRemoveAmount(float count)
    {
        float value = amount - count;
        float overflow = value < 0 ? Mathf.Abs(value) : 0;

        return count - overflow;
    }

    //  Removes count and returns how much was removed
    public float TryRemove(float count)
    {
        amount -= count;

        float overflow = amount < 0 ? Mathf.Abs(amount) : 0;

        if (amount <= 0)
        {
            UnbakeFromGrid();
            Destroy(this.gameObject);
        }

        return count - overflow;
    }
}
