using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class QueueUnitButton : MonoBehaviour
{    
    public RTSUnitType unitTypeToQueue;
    public GameObject buttonLockedObject;

    [SerializeField]
    private bool locked;
    protected BuildingSpawnQueue buildingSpawnQueue;
    HoverButton hoverButton;
    void Start()
    {
        buildingSpawnQueue = GetComponentInParent<BuildingSpawnQueue>();

        hoverButton = GetComponentInChildren<HoverButton>();
        hoverButton.onButtonDown.AddListener(OnButtonDown);
    }

    public void Lock()
    {
        buttonLockedObject.SetActive(true);
        hoverButton.enabled = false;
        locked = true;
    }

    public void UnLock()
    {
        buttonLockedObject.SetActive(false);
        hoverButton.enabled = true;
        locked = false;
    }

    // TODO: Switch this to use C# event rather than HandEvent which inherits from
    // UnityEvent.
    public void OnButtonDown(Hand hand)
    {   
        buildingSpawnQueue.QueueUnit(unitTypeToQueue);
    }
}
