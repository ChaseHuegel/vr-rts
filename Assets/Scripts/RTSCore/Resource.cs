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

    public GameObject onDestroyPrefab;
    public override void Initialize()
    {
        base.Initialize();
        AllResources.Add(this);
    }

    protected override void OnDestroy()
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

            if (onDestroyPrefab)
                Instantiate(onDestroyPrefab, this.transform.position, Quaternion.identity);

            Destroy(gameObject);
        }

        return count - overflow;
    }
}
