using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Valve.VR.Extras;
using Valve.VR.InteractionSystem;

public class BuildMenu : MonoBehaviour
{
    public bool enableDetailPanel = true;
    public GameObject detailPanel;
    public TMPro.TextMeshPro titleText;
    public TMPro.TextMeshPro descriptionText;
    public TMPro.TextMeshPro detailText;
    private BuildMenuTab[] buildMenuTabs;
    private int activeTabIndex = 0;

    void Awake()
    {
        if (TryGetTabs())
            InitializeTabs();
    }

    void Start()
    {        
        HookIntoEvents();
        SetActiveTab(0);
    }
    
    private void InitializeTabs()
    {
        foreach (BuildMenuTab tab in buildMenuTabs)
        {
            tab.gameObject.SetActive(true);
            tab.Generate();
            tab.Initialize();

            foreach (BuildMenuSlot slot in tab.Slots)
            {
                slot.HandHoverBegin += OnSlotHandHoverBegin;
                // slot.HandHoverEnd += OnSlotHandHoverEnd;
            }
            
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
}
