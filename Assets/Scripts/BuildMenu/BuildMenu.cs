using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Valve.VR.Extras;

public class BuildMenu : MonoBehaviour
{
    public BuildMenuTab[] tabs;

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

    void Start()
    {
    }
}
