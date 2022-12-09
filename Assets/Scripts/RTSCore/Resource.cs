using System;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;

public class Resource : Obstacle
{
    public readonly static List<Resource> AllResources = new();
    public ResourceGatheringType type = ResourceGatheringType.None;
    public float yield = 1000;

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
    
    //  Removes count and returns how much was removed
    public virtual float TryRemove(float count)
    {
        yield -= count;

        float overflow = yield < 0 ? Mathf.Abs(yield) : 0;

        if (yield <= 0)
        {
            UnbakeFromGrid();

            if (onDestroyPrefab)
                Instantiate(onDestroyPrefab, this.transform.position, Quaternion.identity);

            Destroy(gameObject);
        }

        return count - overflow;
    }
}
