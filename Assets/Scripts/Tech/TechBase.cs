using System.Collections;
using System.Collections.Generic;
using Swordfish.Library.Collections;
using UnityEngine;
using UnityEngine.Events;

//[CreateAssetMenu(menuName = "RTS/Tech/New Tech")]
public class TechBase : ScriptableObject
{
    public string title;
    public string description;

    [Tooltip("For units, the time to produce a unit after it's queued, otherwise it's the research time for a technology.")]
    public float queueResearchTime = 25.0f;

    [Header("World Visuals")]
    // TODO: Optimize - Not used by all derived class, could be moved later on to save on memory.
    public GameObject worldPrefab; 

    [Tooltip("The cube map material used to skin the buttons in the interaction panels for buildings.")]
    public Material worldButtonMaterial;

    [Tooltip("The image used in the queue list display of building interaction panels.")]
    public Sprite worldQueueImage;

    [Header("Economic Costs")]
    // TODO: Optimize - Not used by all derived class, could be moved later on to save on memory.
    public int populationCost;
    public int goldCost;
    public int stoneCost;
    public int foodCost;
    public int woodCost;
    
    public virtual void Execute(SpawnQueue spawnQueue)
    {
        PlayerManager.Instance.ProcessTechQueueComplete(this);
    }
}


