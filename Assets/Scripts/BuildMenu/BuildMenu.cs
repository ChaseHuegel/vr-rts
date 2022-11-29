using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Valve.VR.Extras;
using Valve.VR.InteractionSystem;

public class BuildMenu : MonoBehaviour
{
    public bool enableDetailPanel = true;
    public GameObject detailRanel;
    public TMPro.TextMeshPro titleText;
    public TMPro.TextMeshPro descriptionText;
    public TMPro.TextMeshPro detailText;
    private BuildMenuTab[] buildMenuTabs;
    private int activeTabIndex = 0;

    void Start()
    {
        TryGetTabs();
        InitializeTabs();
        SetActiveTab(0);
        HookIntoEvents();
    }
    
    private void InitializeTabs()
    {
        // Init each tab
        foreach (BuildMenuTab tab in buildMenuTabs)
        {
            tab.Initialize();
            tab.gameObject.SetActive(false);
        }
    }

    private void OnSlotHandHoverBegin(TechBase techBase)
    {
        if (titleText)
            titleText.text = techBase.title;

        if (descriptionText)
            descriptionText.text = techBase.description;

        if (detailText)
            detailText.text = techBase.details;
    }

    // private void OnSlotHandHoverEnd(TechBase techBase)
    // {
    //     if (titleText)
    //         titleText.text = "";

    //     if (descriptionText)
    //         descriptionText.text = "";

    //     if (detailText)
    //         detailText.text = "";
    // }

    private void HookIntoEvents()
    {
        foreach (BuildMenuTab tab in buildMenuTabs)
        {
            foreach (BuildMenuSlot slot in tab.Slots)
            {
                slot.HandHoverBegin += OnSlotHandHoverBegin;
                // slot.HandHoverEnd += OnSlotHandHoverEnd;
            }
        }
    }

    private void CleanupEvents()
    {
        if (buildMenuTabs == null)
            return;
            
        foreach (BuildMenuTab tab in buildMenuTabs)
        {
            foreach (BuildMenuSlot slot in tab.Slots)
            {
                slot.HandHoverBegin -= OnSlotHandHoverBegin;
                // slot.HandHoverEnd -= OnSlotHandHoverEnd;
            }
        }
    }

    private void OnDestroy()
    {
        CleanupEvents();
    }

    private bool TryGetTabs()
    {
        buildMenuTabs = GetComponentsInChildren<BuildMenuTab>(true);

        if (buildMenuTabs.Length <= 0)
            return false;

        return true;
    }

    public void NextTab()
    {        
        buildMenuTabs[activeTabIndex]?.gameObject.SetActive(false);

        activeTabIndex++;
        if (activeTabIndex >= buildMenuTabs.Length)
            activeTabIndex = 0;

        buildMenuTabs[activeTabIndex].gameObject.SetActive(true);

    }

    public void PreviousTab()
    {
        buildMenuTabs[activeTabIndex].gameObject.SetActive(false);

        activeTabIndex++;
        if (activeTabIndex >= buildMenuTabs.Length)
            activeTabIndex = 0;

        buildMenuTabs[activeTabIndex].gameObject.SetActive(true);
    }

    public void SetActiveTab(int newTabIndex)
    {
        buildMenuTabs[activeTabIndex].gameObject.SetActive(false);
        activeTabIndex = newTabIndex;
        buildMenuTabs[activeTabIndex].gameObject.SetActive(true);
    }

    public void GenerateTabs()
    {
        foreach(BuildMenuTab tab in buildMenuTabs)
            tab.Generate();
    }
}
