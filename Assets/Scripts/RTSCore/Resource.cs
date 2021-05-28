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
    public List<Actor> interactors;

    // ! Can probably remove this if the new functionality works out.
    public bool IsBusy()//Unit unit = null)
    {
        // if (interactors.Contains(unit))
        //     return false;

        return (maxInteractors > 0 && interactors.Count >= maxInteractors);
    }

    /// <summary>
    /// Returns true (can interact) if unit is already slotted at this
    /// resource or is added to the available slots. Returns false 
    /// if unit can not interact with this resource. Returns true if
    /// maxInteractors is set to 0 (unlimited).
    /// </summary>
    /// <param name="actor">The unit requesting interaction.</param>
    /// <returns>True if unit can interact, false otherwise.</returns>
    public bool AddInteractor(Actor actor)
    {
        if (maxInteractors == 0)
            return true;

        if (interactors.Contains(actor))
            return true;

        if (interactors.Count < maxInteractors)
        {
            interactors.Add(actor);
            return true;
        }

        return false;
    }

    public void RemoveInteractor(Actor actor)
    {
        interactors.Remove(actor);
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
