using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Valve.VR.Extras;
using Valve.VR.InteractionSystem;

public class BuildMenu : MonoBehaviour
{
    public BuildMenuTab[] tabs;

    private int activeTabIndex = 0;

    void Start()
    {
    }
    
    public void OnAttachedToHand(Hand hand)
    {
        //RefreshSlots();
    }

    public void NextTab()
    {        
        tabs[activeTabIndex].gameObject.SetActive(false);

        activeTabIndex++;
        if (activeTabIndex >= tabs.Length)
            activeTabIndex = 0;

        tabs[activeTabIndex].gameObject.SetActive(true);
    }

    public void PreviousTab()
    {
        tabs[activeTabIndex].gameObject.SetActive(false);

        activeTabIndex++;
        if (activeTabIndex >= tabs.Length)
            activeTabIndex = 0;

        tabs[activeTabIndex].gameObject.SetActive(true);
    }

    public void SetActiveTab(int newTabIndex)
    {
        tabs[activeTabIndex].gameObject.SetActive(false);
        activeTabIndex = newTabIndex;
        tabs[activeTabIndex].gameObject.SetActive(true);
    }

    public void GenerateTabs()
    {
        foreach(BuildMenuTab tab in tabs)
        {
            tab.Generate();
        }
    }
}
