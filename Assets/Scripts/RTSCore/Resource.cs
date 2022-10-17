using System;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

public class Resource : Obstacle
{
    public readonly static List<Resource> AllResources = new();

    public ResourceGatheringType type = ResourceGatheringType.None;
    public int amount = 1000;

    public int maxInteractors = 0;
    //public int interactors = 0;
    public List<Actor> interactors;

    public override void Initialize()
    {
        base.Initialize();
        AllResources.Add(this);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        AllResources.Remove(this);
    }

    // ! No info in database aboun resource bounding dimensions, has to be 
    // ! set in the prefab.
    // public override void FetchBoundingDimensions()
    // {
    //     base.FetchBoundingDimensions();

    //     boundingDimensions.x = buildingData.boundingDimensionX;
    //     boundingDimensions.y = buildingData.boundingDimensionY;
    // }

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
