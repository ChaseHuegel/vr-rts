using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public PalmMenu palmMenu;

    public WristDisplay WristDisplay;

    public int woodCollected;
    public int goldCollected;
    public int grainCollected;

    public int civilianPopulation;
    public int militaryPopulation;
    public int totalPopulation;
    public int populationLimit;

    private TeleportArc teleportArc;

    public SteamVR_Action_Boolean palmMenuOnOff;

    List<RTSUnitTypeData> rtsUnitDataList = new List<RTSUnitTypeData>();
    
    public GameObject villagerPrefab;
    public Sprite builderWorldButtonImage;
    public Sprite minerWorldButtonImage;
    public Sprite farmerWorldButtonImage;
    public Sprite lumberjackWorldButtonImage;

    // Start is called before the first frame update
    void Start()
    {
        teleportArc = this.GetComponent<TeleportArc>();

        // Move this to a function on the player?
        // Player.instance.rightHand.GetComponent<HandPhysics>().enabled = false;
        // Player.instance.leftHand.GetComponent<HandPhysics>().enabled = false;
        
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

        RTSUnitTypeData builderData = new RTSUnitTypeData(RTSUnitType.Builder, 5.0f, villagerPrefab, builderWorldButtonImage, 1);
        RTSUnitTypeData farmerData = new RTSUnitTypeData(RTSUnitType.Farmer, 5.0f, villagerPrefab, farmerWorldButtonImage, 1);
        RTSUnitTypeData lumberjackData = new RTSUnitTypeData(RTSUnitType.Lumberjack, 5.0f, villagerPrefab, lumberjackWorldButtonImage, 1);
        RTSUnitTypeData minerData = new RTSUnitTypeData(RTSUnitType.GoldMiner, 5.0f, villagerPrefab, minerWorldButtonImage, 1);
        
        rtsUnitDataList.Add(builderData);
        rtsUnitDataList.Add(farmerData);
        rtsUnitDataList.Add(lumberjackData);
        rtsUnitDataList.Add(minerData);
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

    public void AddToPopulation(RTSUnitType unitType)
    {
        // Determine if the unit should be added to civilian or military population
        if (unitType == RTSUnitType.Builder || unitType == RTSUnitType.Farmer || unitType == RTSUnitType.Lumberjack ||
            unitType == RTSUnitType.OreMiner || unitType == RTSUnitType.GoldMiner)
        {
            civilianPopulation += 1;
            WristDisplay.SetCivilianPopulationText(civilianPopulation.ToString());
        }
        else
        {
            militaryPopulation += 1;
            WristDisplay.SetMilitaryPopulationText(militaryPopulation.ToString());
        }
        
        totalPopulation += 1;
        WristDisplay.SetTotalPopulationText(totalPopulation.ToString() + "/" + populationLimit.ToString());
    }

    public void RemoveFromPopulation(RTSUnitType unitType)
    {
        // Determine if the unit should be removed from civilian or military population
        if (unitType == RTSUnitType.Builder || unitType == RTSUnitType.Farmer || unitType == RTSUnitType.Lumberjack ||
            unitType == RTSUnitType.OreMiner)
        {
            civilianPopulation -= 1;
            WristDisplay.SetCivilianPopulationText(civilianPopulation.ToString());
        }
        else
        {
            militaryPopulation -= 1;
            WristDisplay.SetMilitaryPopulationText(militaryPopulation.ToString());            
        }

        totalPopulation -= 1;
        WristDisplay.SetTotalPopulationText(totalPopulation.ToString() + "/" + populationLimit.ToString());
    }

    public void IncreasePopulationLimit(int amountToIncreaseBy)
    {
        populationLimit += amountToIncreaseBy;   
    }

    public void DecreasePopulationLimit(int amountDecreaseBy)
    {
        populationLimit -= amountDecreaseBy;
    }

    public void AddResourceToStockpile(ResourceGatheringType type, int amount)
    {
        switch (type)
        {
            case ResourceGatheringType.Wood:
                AddWoodToResources(amount);
                break;

            case ResourceGatheringType.Grain:
                AddGrainToResources(amount);
                break;

            case ResourceGatheringType.Gold:
                AddGoldToResources(amount);
                break;

            default:
                break;
        }
    }

    public void AddWoodToResources(int amount)
    {
        woodCollected += amount;
        WristDisplay.SetWoodText(woodCollected.ToString());
    }
    public void AddGrainToResources(int amount)
    {
        grainCollected += amount;
        WristDisplay.SetGrainText(grainCollected.ToString());
    }
    public void AddGoldToResources(int amount)
    {
        goldCollected += amount;
        WristDisplay.SetGoldText(goldCollected.ToString());
    }
}
