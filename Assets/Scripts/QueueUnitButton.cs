using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class QueueUnitButton : MonoBehaviour
{
    public TechBase techToQueue;
    public GameObject buttonLockedObject;

    [SerializeField]
    private bool locked;

    HoverButton hoverButton;

    //protected SpawnQueue buildingSpawnQueue;

    void Start()
    {
        hoverButton = GetComponentInChildren<HoverButton>();
        // buildingSpawnQueue = GetComponentInParent<BuildingSpawnQueue>();
        // hoverButton.onButtonDown.AddListener(OnButtonDown);
    }

    // TODO: Optimize - Instantiate lock object when needed rather than every button
    // having an inactive object?
    public void SetLocked(bool locked)
    {
        if (!hoverButton)
            hoverButton = GetComponentInChildren<HoverButton>();
            
        if (locked)
        {
            buttonLockedObject.SetActive(true);
            hoverButton.enabled = false;
            locked = true;
        }
        else
        {
            buttonLockedObject.SetActive(false);
            hoverButton.enabled = true;
            locked = false;
        }

    }

    // // TODO: Switch this to use C# event rather than HandEvent which inherits from
    // // UnityEvent.
    // public void OnButtonDown(Hand hand)
    // {   
    //     buildingSpawnQueue.QueueUnit(unitTypeToQueue);
    // }
}
