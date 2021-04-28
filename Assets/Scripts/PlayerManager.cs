using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PlayerManager : MonoBehaviour
{
    public PalmMenu palmMenu;

    public WristDisplay WristDisplay;

    int woodCollected;

    private TeleportArc teleportArc;

    public SteamVR_Action_Boolean palmMenuOnOff;

    List<RTSUnitTypeData> rtsUnitDataList = new List<RTSUnitTypeData>();
    
    public GameObject villagerPrefab;
    
    // Start is called before the first frame update
    void Start()
    {
        teleportArc = this.GetComponent<TeleportArc>();
        Player.instance.rightHand.GetComponent<HandPhysics>().enabled = false;
        Player.instance.leftHand.GetComponent<HandPhysics>().enabled = false;
        if (palmMenu == null)
        {
            palmMenu = Player.instance.GetComponent<PalmMenu>();
        }

        palmMenuOnOff.AddOnStateDownListener(TogglePalmMenu, SteamVR_Input_Sources.RightHand);
        palmMenuOnOff.AddOnStateUpListener(TogglePalmMenu, SteamVR_Input_Sources.RightHand);
        palmMenuOnOff.AddOnStateDownListener(TogglePalmMenu, SteamVR_Input_Sources.LeftHand);
        palmMenuOnOff.AddOnStateUpListener(TogglePalmMenu, SteamVR_Input_Sources.LeftHand);

        teleportArc.Show();

        rtsUnitDataList = new List<RTSUnitTypeData>();

        RTSUnitTypeData villagerData = new RTSUnitTypeData(RTSUnitType.Villager, 3.0f, villagerPrefab);

        rtsUnitDataList.Add(villagerData);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public RTSUnitTypeData FindUnitData(RTSUnitType type)
    {
        RTSUnitTypeData ret = rtsUnitDataList.Find(x => x.unitType == type );
        return ret;
    }

    public void TogglePalmMenu(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        palmMenu.Toggle();        
    }

    public void AddWoodToResources(int amount)
    {
        woodCollected += amount;
        WristDisplay.SetWoodText(woodCollected.ToString());
    }
}
