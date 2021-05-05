using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Valve.VR.Extras;
using Valve.VR.InteractionSystem;

public class BuildMenu : MonoBehaviour
{
    public BuildMenuTab[] tabs;

    void Start()
    {
        
    }

    void OnEnable()
    {
        foreach(BuildMenuTab tab in tabs)
        {
            foreach(BuildMenuSlot slot in tab.GetComponentsInChildren<BuildMenuSlot>())
            {
                bool canBuild = PlayerManager.instance.CanConstructBuilding(slot.rtsBuildingType);                
                slot.GetComponentInChildren<SphereCollider>().enabled = canBuild;   
                Debug.Log("enabled");          
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
    }

    public void GenerateTabs()
    {
        foreach(BuildMenuTab tab in tabs)
        {
            tab.Generate();
        }
    }
}
