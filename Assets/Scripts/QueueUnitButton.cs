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

    // TODO: Optimize - Instantiate lock object when needed rather than every button
    // having an inactive object?
    // public void SetLocked(bool locked)
    // {
    //     if (!hoverButton)
    //         hoverButton = GetComponentInChildren<HoverButton>();
            
    //     if (locked)
    //     {
    //         buttonLockedObject.SetActive(true);
    //         hoverButton.enabled = false;
    //         locked = true;
    //     }
    //     else
    //     {
    //         buttonLockedObject.SetActive(false);
    //         hoverButton.enabled = true;
    //         locked = false;
    //     }

    // }

    public void Initialize()
    {
        if (!hoverButton)
            hoverButton = GetComponentInChildren<HoverButton>();

        // buildingSpawnQueue = GetComponentInParent<BuildingSpawnQueue>();
        // hoverButton.onButtonDown.AddListener(OnButtonDown);

        HookIntoEvents();
    }

    public void Lock()
    {
        //Initialize();
        buttonLockedObject.SetActive(true);
        hoverButton.enabled = false;
    }

    public void Unlock()
    {
        //Initialize();
        buttonLockedObject.SetActive(false);
        hoverButton.enabled = true;
    }

    public void Enable()
    {
        // TODO: Sort this out so it's not neccassary here
        //Initialize();
        buttonLockedObject.SetActive(false);
        hoverButton.enabled = true;
    }

    public void Disable()
    {
        // TODO: Sort this out so it's not neccassary here
        //Initialize();
        buttonLockedObject.SetActive(true);
        hoverButton.enabled = false;
    }

    private void OnNodeLocked(TechNode node)
    {
        if (node.tech == techToQueue)
            Lock();
    }

    private void OnNodeUnlocked(TechNode node)
    {
        if (node.tech == techToQueue)
            Unlock();
    }

    private void OnNodeEnabled(TechNode node)
    {
        if (node.tech == techToQueue)
            Enable();
    }

    private void OnNodeDisabled(TechNode node)
    {
        if (node.tech == techToQueue)
            Disable();
    }

    private void OnNodeResearched(TechNode node)
    {
    }
    
    private void HookIntoEvents()
    {
        TechTree.OnNodeUnlocked += OnNodeUnlocked;
        TechTree.OnNodeLocked += OnNodeLocked;
        TechTree.OnNodeEnabled += OnNodeEnabled;
        TechTree.OnNodeDisabled += OnNodeDisabled;
        TechTree.OnNodeResearched += OnNodeResearched;
    }

    private void CleanupEvents()
    {
        TechTree.OnNodeUnlocked -= OnNodeUnlocked;
        TechTree.OnNodeLocked -= OnNodeLocked;
        TechTree.OnNodeEnabled -= OnNodeEnabled;
        TechTree.OnNodeDisabled -= OnNodeDisabled;
        TechTree.OnNodeResearched -= OnNodeResearched;
    }

    void OnDestroy()
    {
        CleanupEvents();
    }

    // // TODO: Switch this to use C# event rather than HandEvent which inherits from
    // // UnityEvent.
    // public void OnButtonDown(Hand hand)
    // {   
    //     buildingSpawnQueue.QueueUnit(unitTypeToQueue);
    // }
}
