using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PlayerManager : MonoBehaviour
{
    public PalmMenu palmMenu;
    
    public SteamVR_Action_Boolean palmMenuOnOff;

    // Start is called before the first frame update
    void Start()
    {
        if (palmMenu == null)
        {
            palmMenu = Player.instance.GetComponent<PalmMenu>();
        }

            palmMenuOnOff.AddOnStateDownListener(TogglePalmMenu, SteamVR_Input_Sources.RightHand);
            palmMenuOnOff.AddOnStateUpListener(TogglePalmMenu, SteamVR_Input_Sources.RightHand);
            palmMenuOnOff.AddOnStateDownListener(TogglePalmMenu, SteamVR_Input_Sources.LeftHand);
            palmMenuOnOff.AddOnStateUpListener(TogglePalmMenu, SteamVR_Input_Sources.LeftHand);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TogglePalmMenu(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        bool palmMenuIsaActive = palmMenu.gameObject.activeSelf;
        if (palmMenuIsaActive)
            palmMenu.gameObject.SetActive(false);
        else
            palmMenu.gameObject.SetActive(true);
    }
}
