using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Valve.VR.Extras;
using Valve.VR.InteractionSystem;

public class BuildMenu : MonoBehaviour
{
    public BuildMenuTab[] tabs;
    protected PlayerManager playerManager;
    void Start()
    {
        playerManager = PlayerManager.instance;
        RefreshSlots();
    }

    public void OnAttachedToHand(Hand hand)
    {
        RefreshSlots();
    }

    // TODO: This could be changed to just update the active tab for
    // performance gains.
    public void RefreshSlots()
    {
        if (!playerManager)
            playerManager = PlayerManager.instance;
            
        foreach(BuildMenuTab tab in tabs)
        {
            foreach(BuildMenuSlot slot in tab.GetComponentsInChildren<BuildMenuSlot>())
            {
                bool canBuild = playerManager.CanConstructBuilding(slot.rtsTypeData.buildingType);                
                slot.SlotEnabled(canBuild); 
            }
        }
    }

    public void SetActiveTab(int tabNumber)
    {
        foreach(BuildMenuTab tab in tabs)
        {
            tab.gameObject.SetActive(false);
        }
        tabs[tabNumber].gameObject.SetActive(true);
        
        RefreshSlots();
    }

    public void GenerateTabs()
    {
        // foreach(BuildMenuTab tab in tabs)
        // {
        //     tab.Generate();
        // }
    }
}
