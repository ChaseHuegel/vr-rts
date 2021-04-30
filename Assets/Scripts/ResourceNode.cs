using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;

public class ResourceNode : MonoBehaviour
{
    public ResourceGatheringType type = ResourceGatheringType.None;
    public string resourceName = "Resource";

    public int maxResourceAmount = 5000;

    public bool destroyWhenResourceDepleted;

    float currentResourceAmount;

    // Start is called before the first frame update
    void Start()
    {
        currentResourceAmount = maxResourceAmount;

        switch (type)
        {
            case ResourceGatheringType.Wood:
                ResourceManager.GetTrees().Add(this);
                break;

            case ResourceGatheringType.Gold:
                ResourceManager.GetGold().Add(this);
                break;

            case ResourceGatheringType.Ore:
                ResourceManager.GetOre().Add(this);
                break;

            case ResourceGatheringType.Grain:
                ResourceManager.GetGrain().Add(this);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void decreaseCurrentResourceAmount(float amountToRemove)
    {
        currentResourceAmount -= amountToRemove;
        //Debug.Log("Removed " + amountToRemove + " " + resourceName + " " + currentResourceAmount + " / " + maxResourceAmount);

        if (currentResourceAmount <= 0)
        {
            //Debug.Log("Resource " + resourceName + " depleted");

            if (destroyWhenResourceDepleted)
            {
                switch (type)
                {
                    case ResourceGatheringType.Wood:
                        ResourceManager.GetTrees().Remove(this);
                        break;

                    case ResourceGatheringType.Gold:
                        ResourceManager.GetGold().Remove(this);
                        break;

                    case ResourceGatheringType.Ore:
                        ResourceManager.GetOre().Remove(this);
                        break;

                    case ResourceGatheringType.Grain:
                        ResourceManager.GetGrain().Remove(this);
                        break;
                }

                GetComponent<Obstacle>().UnbakeFromGrid();
                Destroy(this.gameObject);
                //Debug.Log("Resource " + resourceName + " object destroyed.");
            }
        }
    }
}
