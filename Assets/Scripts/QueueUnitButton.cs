using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class QueueUnitButton : MonoBehaviour
{
    public TechBase techToQueue;
    public GameObject buttonLockedObject;

    public bool IsButtonUnlocked
    {
        get { return !buttonLockedObject.activeSelf; }
    }
    public bool IsButtonEnabled
    {
        get { return hoverButton.enabled; }
    }

    private HoverButton hoverButton;

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
        buttonLockedObject.SetActive(true);
        hoverButton.enabled = false;
    }

    public void Unlock()
    {
        buttonLockedObject.SetActive(false);
        hoverButton.enabled = true;
    }

    public void Enable()
    {
        buttonLockedObject.SetActive(false);
        hoverButton.enabled = true;
    }

    public void Disable()
    {
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

    // private void OnNodeResearched(TechNode node)
    // {
    // }
    
    private void HookIntoEvents()
    {
        TechTree.OnNodeUnlocked += OnNodeUnlocked;
        TechTree.OnNodeLocked += OnNodeLocked;
        TechTree.OnNodeEnabled += OnNodeEnabled;
        TechTree.OnNodeDisabled += OnNodeDisabled;
        // TechTree.OnNodeResearched += OnNodeResearched;
    }

    private void CleanupEvents()
    {
        TechTree.OnNodeUnlocked -= OnNodeUnlocked;
        TechTree.OnNodeLocked -= OnNodeLocked;
        TechTree.OnNodeEnabled -= OnNodeEnabled;
        TechTree.OnNodeDisabled -= OnNodeDisabled;
        // TechTree.OnNodeResearched -= OnNodeResearched;
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
