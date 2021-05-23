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
    public int queueCount;

    [Header("UI")]
    [Tooltip("Switch between clipboard build menu or palm build menu.")]
    public bool usePalmMenu;        
    public bool hammerOnLeft = true;
    public bool hammerOnRight = false;

    public Transform rHandHammerAttachmentpoint;
    public Transform lHandHammerAttachmentPoint;
     public PalmMenu palmMenu;
    public Transform rHandPalmUpAttachmentPoint;
    public Transform rHandPalmUpTrackingPoint;
    public Transform lHandPalmUpAttachmentPoint;
    public Transform lHandPalmUpTrackingPoint;

    public WristDisplay WristDisplay;
    public SteamVR_Action_Boolean handMenuToggle;
    private GripPan gripPan;
    private Hand buildMenuHand;
    private Hand selectionHand;
    public GameObject handBuildMenu;
    protected BuildMenu buildMenu;
    public byte factionID = 0;

    private Hand rightHand;
    private Hand leftHand;

    private static PlayerManager _instance;
    public static PlayerManager instance
    {
        get
        {
            if ( _instance == null )
                _instance = GameObject.FindObjectOfType<PlayerManager>();

            return _instance;
        }
    }

    void Awake()
    {
        _instance = this;
        if (!(gripPan = Player.instance.GetComponent<GripPan>()))
            Debug.Log("GripPan not found.");
    }

    void Start()
    {
        HookIntoEvents();

        rightHand = Player.instance.rightHand;
        leftHand = Player.instance.leftHand;

        WristDisplay?.SetWoodText(woodCollected.ToString());
        WristDisplay?.SetGrainText(grainCollected.ToString());
        WristDisplay?.SetGoldText(goldCollected.ToString());
        WristDisplay?.SetStoneText(stoneCollected.ToString());

        if (palmMenu == null) { palmMenu = Player.instance.GetComponent<PalmMenu>(); }

        // Initialize hammer position
        lHandHammerAttachmentPoint.gameObject.SetActive(hammerOnLeft);
        rHandHammerAttachmentpoint.gameObject.SetActive(hammerOnRight);

        buildMenu = handBuildMenu.GetComponent<BuildMenu>();

        handMenuToggle?.AddOnStateDownListener(OnHandToggleMenuRightDown, SteamVR_Input_Sources.RightHand);
        handMenuToggle?.AddOnStateDownListener(OnHandToggleMenuLeftDown, SteamVR_Input_Sources.LeftHand);
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

    public void OnVillagerDropoff(object sender, Villager.DropoffEvent e)
    {
        if (e.villager.factionID == factionID)
            AddResourceToStockpile(e.resourceType, (int)e.amount);
    }

    public void OnVillagerRepair(object sender, Villager.RepairEvent e)
    {
        if (e.villager.factionID == factionID)
            RemoveResourcesFromStockpile(1, 1, 1, 1);
    }

    public void HookIntoEvents()
    {
        Villager.OnDropoffEvent += OnVillagerDropoff;
        Villager.OnRepairEvent += OnVillagerRepair;
    }

    public void CleanupEvents()
    {
        Villager.OnDropoffEvent -= OnVillagerDropoff;
        Villager.OnRepairEvent -= OnVillagerRepair;
    }

    protected bool IsClipboardPalmMenuVisible;

    public void OnHandToggleMenuRightDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        buildMenu.RefreshSlots();

        // Don't use both methods to display the menu at the same time.
        if (usePalmMenu) return;

        // Menu is already visible.
        if (handBuildMenu.activeSelf)
        {
            // Menu is attached to right hand, deactivate it, detach it, and
            // enable interactions in right hand.
            if (rightHand.currentAttachedObject == handBuildMenu)
            {
                rightHand.DetachObject(handBuildMenu);
                handBuildMenu.SetActive(false);
                SetRightHandInteraction(true);                
            }
            // Menu must be attached to left hand, disable interaction in the right hand,
            // attach it to the right hand, enable interacition in the left hand.
            else
            {
                SetRightHandInteraction(false);
                rightHand.AttachObject(handBuildMenu, GrabTypes.Scripted);
                SetLeftHandInteraction(true);
            }
        }
        // Menu is not visible.
        else
        {
            // Disable interaction in right hand, activate the build menu, and 
            // attach build menu to right hand,
            SetRightHandInteraction(false);
            handBuildMenu.SetActive(true);
            rightHand.AttachObject(handBuildMenu, GrabTypes.Scripted);            
        }

    }

    public void OnHandToggleMenuLeftDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        buildMenu.RefreshSlots();
        
        // Don't use both methods to display the menu at the same time.
        if (usePalmMenu) return;

        // Menu is already visible.
        if (handBuildMenu.activeSelf)
        {
            // Menu is attached to left hand, deactivate it, detach it, and
            // enable interactions in left hand.
            if (leftHand.currentAttachedObject == handBuildMenu)
            {
                leftHand.DetachObject(handBuildMenu);
                handBuildMenu.SetActive(false);
                SetLeftHandInteraction(true);                
            }
            // Menu must be attached to right hand, disable interaction in the left hand,
            // attach it to the left hand, enable interacition in the right hand.
            else
            {
                SetLeftHandInteraction(false);
                leftHand.AttachObject(handBuildMenu, GrabTypes.Scripted);
                SetRightHandInteraction(true);
            }
        }
        // Menu is not visible.
        else
        {
            // Disable interaction in left hand, activate the build menu, and 
            // attach build menu to left hand,
            SetLeftHandInteraction(false);
            handBuildMenu.SetActive(true);
            leftHand.AttachObject(handBuildMenu, GrabTypes.Scripted);            
        }

    }

    public void SetRightHandInteraction(bool canInteract)
    {
        rightHand.useHoverSphere = rightHand.useFingerJointHover = canInteract;
    }

    public void SetLeftHandInteraction(bool canInteract)
    {
        leftHand.useHoverSphere = leftHand.useFingerJointHover = canInteract;
    }

    public void DisableGripPanning(Hand hand) { gripPan.DisablePanning(hand); }

    public void EnableGripPanning(Hand hand) { gripPan.EnablePanning(hand); }

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
        
        totalPopulation += unit.rtsUnitTypeData.populationCost;
        queueCount -= unit.rtsUnitTypeData.populationCost;
        if (queueCount < 0) queueCount = 0;
        UpdateWristDisplayPopulationLimit();
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

        totalPopulation -= unit.rtsUnitTypeData.populationCost;
        UpdateWristDisplayPopulationLimit();
    }

    public void IncreasePopulationLimit(int amountToIncreaseBy)
    {
        populationLimit += amountToIncreaseBy;
        UpdateWristDisplayPopulationLimit();
    }

    public void RemoveFromQueueCount(int amount = 1)
    {
        queueCount -= amount;
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
                WristDisplay?.SetStoneText(stoneCollected.ToString());
                break;

            default:
                break;
        }

        buildMenu.RefreshSlots();
    }

    public void RemoveResourcesFromStockpile(int gold, int grain, int wood, int stone)
    {
        goldCollected -= gold;
        grainCollected -= grain;
        woodCollected -= wood;
        stoneCollected -= stone;

        UpdateWristDisplayResourceText();
        buildMenu.RefreshSlots();
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
        UnitData unitData = GameMaster.GetUnit(unitType);
        if (goldCollected < unitData.goldCost || woodCollected < unitData.woodCost ||
            grainCollected < unitData.grainCost || stoneCollected < unitData.stoneCost)
        {
            return false;
        }

        if (unitData.populationCost + totalPopulation + queueCount > populationLimit)
        {
            return false;
        }

        queueCount += unitData.populationCost;
        return true;
    }

    public void RemoveUnitQueueCostFromStockpile(UnitData unitType)
    {
        woodCollected -= unitType.woodCost;
        grainCollected -= unitType.grainCost;
        goldCollected -= unitType.goldCost;
        stoneCollected -= unitType.stoneCost;

        UpdateWristDisplayResourceText();
    }
    public void OnDestroy()
    {        
        CleanupEvents();
    }
}
