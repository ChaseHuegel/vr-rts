using System;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

public class Resource : Obstacle
{
    public readonly static List<Resource> AllResources = new();

    public ResourceGatheringType type = ResourceGatheringType.None;
    public float amount = 1000;

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
            Destroy(gameObject);
        }

        return count - overflow;
    }
}
