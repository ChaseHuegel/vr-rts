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
    [Tooltip("Switch between clipboard build menu or palm build menu.")]
    public bool usePalmMenu;     public PalmMenu palmMenu;
    public Transform rHandClipboardAttachmentPoint;
    public Transform rHandPalmUpAttachmentPoint;
    public Transform rHandPalmUpTrackingPoint;
    public Transform lHandClipboardAttachmentPoint;
    public Transform lHandPalmUpAttachmentPoint;
    public Transform lHandPalmUpTrackingPoint;
    public WristDisplay WristDisplay;
    public SteamVR_Action_Boolean handMenuToggle;
    private GripPan gripPan;
    private Hand buildMenuHand;
    private Hand selectionHand;
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

        handMenuToggle?.AddOnStateDownListener(OnToggleHandMenu, SteamVR_Input_Sources.RightHand);
        handMenuToggle?.AddOnStateUpListener(OnToggleHandMenu, SteamVR_Input_Sources.RightHand);
        handMenuToggle?.AddOnStateDownListener(OnToggleHandMenu, SteamVR_Input_Sources.LeftHand);
        handMenuToggle?.AddOnStateUpListener(OnToggleHandMenu, SteamVR_Input_Sources.LeftHand);
    }

    void Update()
    {
        //Vector3 direction = (Player.instance.hmdTransform.position - origin.position).normalized;
        //float facing = Vector3.Dot(origin.right, direction);
        if (usePalmMenu)
        {
            if (lHandPalmUpTrackingPoint.right.y > 0.30f)
                palmMenu.Show();
            else
                palmMenu.Hide();
        }
    }

    protected bool IsClipboardPalmMenuVisible;
    public void OnToggleHandMenu(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {

        // Don't use both methods to display the menu at the same time.
        if (usePalmMenu)
            return;

        // Menu isn't visible, lets see which hand is going to display it.
        if (!IsClipboardPalmMenuVisible)
        {
            if (fromSource == SteamVR_Input_Sources.RightHand)
            {Debug.Log("right");
                rHandClipboardAttachmentPoint.gameObject.SetActive(true);
                buildMenuHand = Player.instance.rightHand;
                selectionHand = Player.instance.leftHand;
            }
            else if (fromSource == SteamVR_Input_Sources.LeftHand)
            {
                lHandClipboardAttachmentPoint.transform.gameObject.SetActive(true);
                buildMenuHand = Player.instance.leftHand;
                selectionHand = Player.instance.rightHand;
            }

            buildMenuHand.useHoverSphere = buildMenuHand.useFingerJointHover = false;
            selectionHand.useHoverSphere = selectionHand.useFingerJointHover = true;
            IsClipboardPalmMenuVisible = true;
        }
        else // IsClipboardPalmMenuVisible = true
        {
            // Deactivite both menus
            lHandClipboardAttachmentPoint.gameObject.SetActive(false);
            rHandClipboardAttachmentPoint.gameObject.SetActive(false);
            buildMenuHand.useHoverSphere = buildMenuHand.useFingerJointHover = true;
            selectionHand.useHoverSphere = selectionHand.useFingerJointHover = true;
            IsClipboardPalmMenuVisible = false;
        }
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

        BuildingData buildingData = GameMaster.GetBuilding(buildingType);
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

        UnitData unitData = GameMaster.GetUnit(unitType);
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

    public void RemoveUnitQueueCostFromStockpile(UnitData unitType)
    {
        woodCollected -= unitType.woodCost;
        grainCollected -= unitType.grainCost;
        goldCollected -= unitType.goldCost;
        stoneCollected -= unitType.stoneCost;

        UpdateWristDisplayResourceText();
    }
}
