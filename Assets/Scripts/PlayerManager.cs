using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [Header("Stats/Resources")]
    public int woodCollected;
    public int goldCollected;
    public int stoneCollected;
    public int grainCollected;
    public int civilianPopulation;
    public int militaryPopulation;
    public int totalPopulation;
    public int populationLimit;

    [Header("UI")]
    public PalmMenu palmMenu;
    public WristDisplay WristDisplay;
    public SteamVR_Action_Boolean palmMenuOnOff;
    private GripPan gripPan;
    private static PlayerManager _instance;
    public static PlayerManager instance
    {
        get
        {
            if ( _instance == null )
            {
                _instance = GameObject.FindObjectOfType<PlayerManager>();
            }

            return _instance;
        }
    }

    public void DisableGripPanning(Hand hand)
    {
        gripPan.DisablePanning(hand);
    }

    public void EnableGripPanning(Hand hand)
    {
        gripPan.EnablePanning(hand);
    }

    void Awake()
    {
        _instance = this;
        if (!(gripPan = Player.instance.GetComponent<GripPan>()))
            Debug.Log("GripPan not found.");
    }

    void Start()
    {
        WristDisplay?.SetWoodText(woodCollected.ToString());
        WristDisplay?.SetGrainText(grainCollected.ToString());
        WristDisplay?.SetGoldText(goldCollected.ToString());
        WristDisplay?.SetStoneText(stoneCollected.ToString());

        if (palmMenu == null)
        {
            palmMenu = Player.instance.GetComponent<PalmMenu>();
        }

        palmMenuOnOff?.AddOnStateDownListener(TogglePalmMenu, SteamVR_Input_Sources.RightHand);
        palmMenuOnOff?.AddOnStateUpListener(TogglePalmMenu, SteamVR_Input_Sources.RightHand);
        palmMenuOnOff?.AddOnStateDownListener(TogglePalmMenu, SteamVR_Input_Sources.LeftHand);
        palmMenuOnOff?.AddOnStateUpListener(TogglePalmMenu, SteamVR_Input_Sources.LeftHand);

    }

    // void Update()
    // {
    //     Transform origin = Player.instance.leftHand.GetComponent<HandTrackingPoint>().transform;
    //     Vector3 direction = (Player.instance.hmdTransform.position - origin.position).normalized;

    //     float facing = Vector3.Dot(origin.right, direction);

    //     Debug.Log("facing - " + facing);

    //     if (facing > 0.90f)
    //     {
    //         palmMenu.Show(Player.instance.leftHand.GetComponent<HandTrackingPoint>().gazeMenuAttachmentPoint);
    //     }
    //     else
    //     {
    //         palmMenu.Hide();
    //     }
    // }

    public void TogglePalmMenu(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        palmMenu.Toggle();
    }

    public void AddToPopulation(Unit unit)
    {
        // Determine if the unit should be added to civilian or military population
        if (unit.IsCivilian())
        {
            civilianPopulation += 1;
            WristDisplay?.SetCivilianPopulationText(civilianPopulation.ToString());
        }
        else
        {
            militaryPopulation += 1;
            WristDisplay?.SetMilitaryPopulationText(militaryPopulation.ToString());
        }

        totalPopulation += 1;
        UpdateWristDisplayPopulationLimit();
    }

    public void RemoveResourcesFromStockpile(int gold, int grain, int wood, int ore)
    {
        goldCollected -= gold;
        grainCollected -= grain;
        woodCollected -= wood;
        stoneCollected -= ore;

        UpdateWristDisplayResourceText();
    }

    public void RemoveFromPopulation(Unit unit)
    {
        // Determine if the unit should be removed from civilian or military population
        if (unit.IsCivilian())
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
        UpdateWristDisplayPopulationLimit();
    }

    public void IncreasePopulationLimit(int amountToIncreaseBy)
    {
        populationLimit += amountToIncreaseBy;
        UpdateWristDisplayPopulationLimit();
    }

    public void DecreasePopulationLimit(int amountDecreaseBy)
    {
        populationLimit -= amountDecreaseBy;
        UpdateWristDisplayPopulationLimit();
    }

    void UpdateWristDisplayPopulationLimit()
    {
        WristDisplay?.SetTotalPopulationText(totalPopulation.ToString() + "/" + populationLimit.ToString());
    }

    void UpdateWristDisplayResourceText()
    {
        WristDisplay?.SetWoodText(woodCollected.ToString());
        WristDisplay?.SetGrainText(grainCollected.ToString());
        WristDisplay?.SetGoldText(goldCollected.ToString());
        WristDisplay?.SetStoneText(stoneCollected.ToString());
    }

    public void AddResourceToStockpile(ResourceGatheringType type, int amount)
    {
        switch (type)
        {
            case ResourceGatheringType.Wood:
                woodCollected += amount;
                WristDisplay?.SetWoodText(woodCollected.ToString());
                break;

            case ResourceGatheringType.Grain:
                grainCollected += amount;
                WristDisplay?.SetGrainText(grainCollected.ToString());
                break;

            case ResourceGatheringType.Gold:
                goldCollected += amount;
                WristDisplay?.SetGoldText(goldCollected.ToString());
                break;

            case ResourceGatheringType.Stone:
                stoneCollected += amount;
                WristDisplay?.SetStoneText(goldCollected.ToString());
                break;

            default:
                break;
        }
    }

    public bool CanConstructBuilding(RTSBuildingType buildingType)
    {
        bool ret = true;

        RTSBuildingTypeData buildingData = GameMaster.Instance.FindBuildingData(buildingType);
        if (goldCollected < buildingData.goldCost || woodCollected < buildingData.woodCost ||
            grainCollected < buildingData.grainCost || stoneCollected < buildingData.stoneCost)
        {
            return false;
        }

        return ret;
    }

    public bool CanQueueUnit(RTSUnitType unitType)
    {
        bool ret = true;

        RTSUnitTypeData unitData = GameMaster.Instance.FindUnitData(unitType);
        if (goldCollected < unitData.goldCost || woodCollected < unitData.woodCost ||
            grainCollected < unitData.grainCost || stoneCollected < unitData.stoneCost)
        {
            return false;
        }

        if (unitData.populationCost + totalPopulation > populationLimit)
        {
            return false;
        }

        return ret;
    }

    public void RemoveUnitQueueCostFromStockpile(RTSUnitTypeData unitType)
    {
        woodCollected -= unitType.woodCost;
        grainCollected -= unitType.grainCost;
        goldCollected -= unitType.goldCost;
        stoneCollected -= unitType.stoneCost;

        UpdateWristDisplayResourceText();
    }
}
