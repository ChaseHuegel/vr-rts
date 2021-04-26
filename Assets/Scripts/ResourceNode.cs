using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    public string resourceName = "Resource";

    public int maxResourceAmount = 5000;    

    public bool destroyWhenResourceDepleted;

    public List<GameObject> PrefabVariations;
    int currentResourceAmount = 5000;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void decreaseCurrentResourceAmount(int amountToRemove)
    {
        currentResourceAmount -= amountToRemove;
        Debug.Log("Removed " + currentResourceAmount + " from " + resourceName + currentResourceAmount + " / " + maxResourceAmount);

        if (currentResourceAmount <= 0)
        {
            Debug.Log("Resource " + resourceName + " depleted");

            if (destroyWhenResourceDepleted)
            {
                Destroy(this);
                Debug.Log("Resource " + resourceName + " object destroy.");
            }
        }
    }
}
