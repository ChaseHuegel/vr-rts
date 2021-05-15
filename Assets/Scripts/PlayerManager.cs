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
    public GameObject handBuildMenu;
    protected BuildMenu buildMenu;

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

        if (palmMenu == null) { palmMenu = Player.instance.GetComponent<PalmMenu>(); }

        // Initialize hammer position
        lHandHammerAttachmentPoint.gameObject.SetActive(hammerOnLeft);
        rHandHammerAttachmentpoint.gameObject.SetActive(hammerOnRight);

        buildMenu = handBuildMenu.GetComponent<BuildMenu>();

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
        buildMenu.RefreshSlots();
        
        // Don't use both methods to display the menu at the same time.
        if (usePalmMenu)
            return;        

        // Menu is visible.
        if (handBuildMenu.activeSelf)
        {
            // Right hand pressed toggle button.
            if (fromSource == SteamVR_Input_Sources.RightHand)
            {
                // Right hand has the menu.
                if (Player.instance.rightHand.currentAttachedObject == handBuildMenu)
                {
                    DetachBuildMenuFromRightHand();

                    // Hide the menu.
                    handBuildMenu.SetActive(false);
                }
                // Right hand doesn't have the menu so attach it to the right hand.
                else
                {
                    ToggleLeftHandInteraction(true);
                    AttachBuildMenuToRightHand();             
                }
            }
            // Left hand pressed toggle button.
            if (fromSource == SteamVR_Input_Sources.LeftHand)
            {   
                // Left hand currently has the menu.
                if (Player.instance.leftHand.currentAttachedObject == handBuildMenu)
                {
                    DetachBuildMenuFromLeftHand();

                    // Hide the menu.
                    handBuildMenu.SetActive(false);
                }
                // Left hand doesn't have the menu so attach it to the Left hand.
                else
                {
                    ToggleRightHandInteraction(true);
                    AttachBuildMenuToLeftHand();
                }
            }
        }
        // Menu is not visible.
        else
        {
            // Right hand pressed toggle button.
            if (fromSource == SteamVR_Input_Sources.RightHand)
            {
                // Right hand doesn't have the menu.
                if (Player.instance.rightHand.currentAttachedObject != handBuildMenu)
                {
                    // Enable interactions
                    ToggleLeftHandInteraction(true);
                }

                // Show the menu.
                handBuildMenu.SetActive(true);
                AttachBuildMenuToRightHand();
            }

            // Left hand pressed toggle button.
            if (fromSource == SteamVR_Input_Sources.LeftHand)
            {           
                // Left hand currently has the menu.
                if (Player.instance.leftHand.currentAttachedObject != handBuildMenu)
                {   
                    // Enable interactions.
                    ToggleRightHandInteraction(true);
                }

                // Show the menu.
                handBuildMenu.SetActive(true);
                AttachBuildMenuToLeftHand();
            }            
        }
    }

    void DetachBuildMenuFromLeftHand()
    {
        // Detach the menu.
        Player.instance.leftHand.DetachObject(handBuildMenu);

        // Enable interactions.
        ToggleLeftHandInteraction(true);
    }

    void DetachBuildMenuFromRightHand()
    {
        // Detach the menu.
        Player.instance.rightHand.DetachObject(handBuildMenu);
        
        // Enable interactions.
        ToggleRightHandInteraction(true);
    }

    void AttachBuildMenuToLeftHand()
    {
        // Disable interactions.
        ToggleLeftHandInteraction(false);
        Player.instance.leftHand.AttachObject(handBuildMenu, GrabTypes.Scripted);

    }

    public void AttachBuildMenuToRightHand()
    {
        // Disable interactions.    
        ToggleRightHandInteraction(false); 
        
        Player.instance.rightHand.AttachObject(handBuildMenu, GrabTypes.Scripted);
    }

    public void ToggleRightHandInteraction(bool canInteract)
    {
        Player.instance.rightHand.useHoverSphere = Player.instance.rightHand.useFingerJointHover = canInteract;
    }

    public void ToggleLeftHandInteraction(bool canInteract)
    {
        Player.instance.leftHand.useHoverSphere = Player.instance.leftHand.useFingerJointHover = canInteract;
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

        totalPopulation += 1;
        queueCount--;
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

        queueCount++;
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
}
