using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class QueueUnitButton : MonoBehaviour
{
    public RTSUnitType unitTypeToQueue;
    protected BuildingSpawnQueue buildingSpawnQueue;
    void Start()
    {
        buildingSpawnQueue = GetComponentInParent<BuildingSpawnQueue>();

        HoverButton hoverButton = GetComponentInChildren<HoverButton>();
        hoverButton.onButtonDown.AddListener(OnButtonDown);
    }

    public void OnButtonDown(Hand hand)
    {
        buildingSpawnQueue.QueueUnit(unitTypeToQueue);
    }
}
