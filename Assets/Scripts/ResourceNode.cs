using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;

public class ResourceNode : MonoBehaviour
{
    public string resourceName = "Resource";

    public int maxResourceAmount = 5000;

    public bool destroyWhenResourceDepleted;

    public List<GameObject> prefabVariations;
    float currentResourceAmount;

    // Start is called before the first frame update
    void Start()
    {
        currentResourceAmount = maxResourceAmount;
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
                GetComponent<Obstacle>().UnbakeFromGrid();
                Destroy(this.gameObject);
                //Debug.Log("Resource " + resourceName + " object destroyed.");
            }
        }
    }
}
