using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildMenu : MonoBehaviour
{
    public GameObject[] tabs;


    public void SetActiveTab(int tabNumber)
    {
        foreach(GameObject tab in tabs)
        {
            tab.SetActive(false);
        }
        tabs[tabNumber].SetActive(true);
    }
}
