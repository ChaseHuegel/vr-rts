using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swordfish;
using Swordfish.Navigation;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PlayerManager : MonoBehaviour
{
    [Header("Stats/Resources")]
    public Faction faction;
    public int woodCollected;
    public int goldCollected;
    public int stoneCollected;
    public int foodCollected;
    public int civilianPopulation;
    public int militaryPopulation;
    public int totalPopulation;
    public int populationLimit;
    public int queueCount;

    [Header("UI")]
    public SteamVR_Action_Boolean handMenuToggle;

    [Header("Hammer")]
    public bool hammerOnLeft = true;
    public bool hammerOnRight = false;
    public Transform rHandHammerAttachmentpoint;
    public Transform lHandHammerAttachmentPoint;

    [Header("Autohide Hand Menu")]
    [Tooltip("Switch between clipboard build menu or palm build menu.")]
    public bool autoHideHandMenuEnabled;
    public float handMenuTrackingSensitivity = 0.5f;
    public Transform rHandAttachmentPoint;
    public Transform rHandTrackingPoint;
    public Transform lHandAttachmentPoint;
    public Transform lHandTrackingPoint;
    public GameObject autohideHandMenuObject;
    public WristDisplay WristDisplay;
    public WristDisplay FaceDisplay;
    private GripPan gripPan;
    private Hand buildMenuHand;
    private Hand selectionHand;
    public GameObject handBuildMenu;
    protected BuildMenu buildMenu;
    private Hand rightHand;
    private Hand leftHand;
    private static PlayerManager _instance;

    public static PlayerManager instance
    {
        get
        {
            if (_instance == null)
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
        WristDisplay?.SetGrainText(foodCollected.ToString());
        WristDisplay?.SetGoldText(goldCollected.ToString());
        WristDisplay?.SetStoneText(stoneCollected.ToString());

        FaceDisplay?.SetWoodText(woodCollected.ToString());
        FaceDisplay?.SetGrainText(foodCollected.ToString());
        FaceDisplay?.SetGoldText(goldCollected.ToString());
        FaceDisplay?.SetStoneText(stoneCollected.ToString());

        if (!autohideHandMenuObject)
            Debug.Log("autohideHandMenuObject not set.", this);

        // Initialize hammer position
        lHandHammerAttachmentPoint.gameObject.SetActive(hammerOnLeft);
        rHandHammerAttachmentpoint.gameObject.SetActive(hammerOnRight);

        buildMenu = handBuildMenu.GetComponent<BuildMenu>();

        handMenuToggle?.AddOnStateDownListener(OnHandToggleMenuRightDown, SteamVR_Input_Sources.RightHand);
        handMenuToggle?.AddOnStateDownListener(OnHandToggleMenuLeftDown, SteamVR_Input_Sources.LeftHand);

        Valve.VR.OpenVR.Chaperone.ResetZeroPose(ETrackingUniverseOrigin.TrackingUniverseStanding);
    }

    private bool isAutohideHandMenuVisible;

    void Update()
    {
        if (autoHideHandMenuEnabled)
        {
            if (lHandTrackingPoint.right.y > handMenuTrackingSensitivity)
            {
                buildMenu.RefreshSlots();
                SetLeftHandInteraction(false);
                isAutohideHandMenuVisible = true;
                autohideHandMenuObject.SetActive(true);
            }
            else
            {
                autohideHandMenuObject.SetActive(false);
                SetLeftHandInteraction(true);
                isAutohideHandMenuVisible = false;
            }
        }
    }

    private float TickTimer = 0f;
    protected void LateUpdate()
    {
        TickTimer += Time.deltaTime;
        if (TickTimer >= Constants.TICK_RATE_DELTA)
        {
            //  TODO need to ensure we aren't accessing anything
            //  in Unity from events that the BTs trigger so we can use Parallel.
            // Parallel.ForEach(ActorV2.AllActors, TickActorBehaviorTree);

            for (int i = 0; i < ActorV2.AllActors.Count; i++)
            {
                ActorV2 actor = ActorV2.AllActors[i];

                if (actor.IsAlive())
                    TickActorBehaviorTree(actor, null, i);

                actor.Tick(Constants.TICK_RATE_DELTA);
            }

            TickTimer -= Constants.TICK_RATE_DELTA;
        }
    }

    private void TickActorBehaviorTree(ActorV2 actor, ParallelLoopState state, long index)
    {
        actor.BehaviorTree?.Tick(actor, Constants.TICK_RATE_DELTA);
    }

    public void HookIntoEvents()
    {
        Damageable.OnDeathEvent += OnDeathEvent;
        Damageable.OnSpawnEvent += OnSpawnEvent;
    }

    public void CleanupEvents()
    {
        Damageable.OnDeathEvent -= OnDeathEvent;
        Damageable.OnSpawnEvent -= OnSpawnEvent;
    }

    public void OnSpawnEvent(object sender, Damageable.SpawnEvent e)
    {
        Body body = e.target.GetComponent<Body>();
        if (body != null && body.Faction != null && body.Faction.IsSameFaction(faction))
        {
            switch (body)
            {
                case Structure structure:
                    IncreasePopulationLimit(structure.buildingData.populationSupported);
                    break;
                case UnitV2 unit:
                    AddToPopulation(unit);
                    break;
            }
        }
    }

    public void OnDeathEvent(object sender, Damageable.DeathEvent e)
    {
        Body body = e.victim.GetComponent<Body>();
        if (body != null && body.Faction.IsSameFaction(faction))
        {
            switch (body)
            {
                case Structure structure:
                    DecreasePopulationLimit(structure.buildingData.populationSupported);
                    break;
                case UnitV2 unit:
                    RemoveFromPopulation(unit);
                    break;
            }
        }
    }

    protected bool IsClipboardPalmMenuVisible;

    public void OnHandToggleMenuRightDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        buildMenu.RefreshSlots();

        // Don't use both methods to display the menu.
        if (autoHideHandMenuEnabled) return;

        // Menu is already visible.
        if (handBuildMenu.activeSelf)
        {
            // Menu is attached to right hand, deactivate it, detach it, and
            // enable interactions in right hand.
            if (rightHand.currentAttachedObject == handBuildMenu)
            {
                rightHand.DetachObject(handBuildMenu);

                // TODO: This should be taken care of by DetachObject -> OnDetachedFromHand event 
                // but OnDetachedFromHand has no reciever (Interactable) for some reason.
                if (rightHand.skeleton != null)
                    rightHand.skeleton.BlendToSkeleton(0.2f);

                handBuildMenu.SetActive(false);
                SetRightHandInteraction(true);
            }
            // Menu must be attached to left hand, disable interaction in the right hand,
            // attach it to the right hand, enable interacition in the left hand.
            else
            {
                // TODO: This should be taken care of by DetachObject -> OnDetachedFromHand event 
                // but OnDetachedFromHand has no reciever (Interactable) for some reason.
                if (leftHand.skeleton != null)
                    leftHand.skeleton.BlendToSkeleton(0.2f);

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
        if (autoHideHandMenuEnabled) return;

        // Menu is already visible.
        if (handBuildMenu.activeSelf)
        {
            // Menu is attached to left hand, deactivate it, detach it, and
            // enable interactions in left hand.
            if (leftHand.currentAttachedObject == handBuildMenu)
            {
                leftHand.DetachObject(handBuildMenu);

                // TODO: This should be taken care of by DetachObject -> OnDetachedFromHand event 
                // but OnDetachedFromHand has no reciever (Interactable) for some reason.
                if (leftHand.skeleton != null)
                    leftHand.skeleton.BlendToSkeleton(0.2f);

                handBuildMenu.SetActive(false);
                SetLeftHandInteraction(true);
            }
            // Menu must be attached to right hand, disable interaction in the left hand,
            // attach it to the left hand, enable interacition in the right hand.
            else
            {
                // TODO: This should be taken care of by DetachObject -> OnDetachedFromHand event 
                // but OnDetachedFromHand has no reciever (Interactable) for some reason.
                if (rightHand.skeleton != null)
                    rightHand.skeleton.BlendToSkeleton(0.2f);

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
        if (rightHand)
            rightHand.useHoverSphere = rightHand.useFingerJointHover = canInteract;
    }

    public void SetLeftHandInteraction(bool canInteract)
    {
        if (leftHand)
            leftHand.useHoverSphere = leftHand.useFingerJointHover = canInteract;
    }

    public void DisableGripPanning(Hand hand)
    {
        gripPan.DisablePanning(hand);
        InteractionPointer.instance.DisableInteraction();
    }

    public void EnableGripPanning(Hand hand)
    {
        gripPan.EnablePanning(hand);
        InteractionPointer.instance.EnableInteraction();
        //InteractionPointer.instance.enabled = true;
    }

    public void AddToPopulation(UnitV2 unit)
    {
        // Determine if the unit should be added to civilian or military population
        if (unit.IsCivilian)
        {
            civilianPopulation += 1;
            WristDisplay?.SetCivilianPopulationText(civilianPopulation.ToString());
            FaceDisplay?.SetCivilianPopulationText(civilianPopulation.ToString());
        }
        else
        {
            militaryPopulation += 1;
            WristDisplay?.SetMilitaryPopulationText(militaryPopulation.ToString());
            FaceDisplay?.SetMilitaryPopulationText(militaryPopulation.ToString());
        }

        totalPopulation += unit.UnitData.populationCost;
        queueCount -= unit.UnitData.populationCost;
        if (queueCount < 0) queueCount = 0;
        UpdateWristDisplayPopulationLimit();
    }

    public void RemoveFromPopulation(UnitV2 unit)
    {
        // Determine if the unit should be removed from civilian or military population
        if (unit.IsCivilian)
        {
            civilianPopulation -= 1;
            WristDisplay.SetCivilianPopulationText(civilianPopulation.ToString());
            FaceDisplay.SetCivilianPopulationText(civilianPopulation.ToString());
        }
        else
        {
            militaryPopulation -= 1;
            WristDisplay.SetMilitaryPopulationText(militaryPopulation.ToString());
            FaceDisplay.SetMilitaryPopulationText(militaryPopulation.ToString());
        }

        totalPopulation -= unit.UnitData.populationCost;
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
        FaceDisplay?.SetTotalPopulationText(totalPopulation.ToString() + "/" + populationLimit.ToString());
    }

    void UpdateWristDisplayResourceText()
    {
        WristDisplay?.SetWoodText(woodCollected.ToString());
        WristDisplay?.SetGrainText(foodCollected.ToString());
        WristDisplay?.SetGoldText(goldCollected.ToString());
        WristDisplay?.SetStoneText(stoneCollected.ToString());

        FaceDisplay?.SetWoodText(woodCollected.ToString());
        FaceDisplay?.SetGrainText(foodCollected.ToString());
        FaceDisplay?.SetGoldText(goldCollected.ToString());
        FaceDisplay?.SetStoneText(stoneCollected.ToString());
    }

    public void AddResourceToStockpile(ResourceGatheringType type, int amount)
    {
        switch (type)
        {
            case ResourceGatheringType.Wood:
                woodCollected += amount;
                WristDisplay?.SetWoodText(woodCollected.ToString());
                FaceDisplay?.SetWoodText(woodCollected.ToString());
                break;

            case ResourceGatheringType.Meat:
            case ResourceGatheringType.Fish:
            case ResourceGatheringType.Grain:
            case ResourceGatheringType.Berries:
                foodCollected += amount;
                WristDisplay?.SetGrainText(foodCollected.ToString());
                FaceDisplay?.SetGrainText(foodCollected.ToString());
                break;

            case ResourceGatheringType.Gold:
                goldCollected += amount;
                WristDisplay?.SetGoldText(goldCollected.ToString());
                FaceDisplay?.SetGoldText(goldCollected.ToString());
                break;

            case ResourceGatheringType.Stone:
                stoneCollected += amount;
                WristDisplay?.SetStoneText(stoneCollected.ToString());
                FaceDisplay?.SetStoneText(stoneCollected.ToString());
                break;

            default:
                break;
        }

        buildMenu.RefreshSlots();
    }

    public void DeductResourcesFromStockpile(int gold, int grain, int wood, int stone)
    {
        goldCollected -= gold;
        foodCollected -= grain;
        woodCollected -= wood;
        stoneCollected -= stone;

        UpdateWristDisplayResourceText();
        buildMenu.RefreshSlots();
    }


    public bool CanConstructBuilding(RTSBuildingType buildingType)
    {
        BuildingData buildingData = GameMaster.GetBuilding(buildingType);
        if (buildingData.goldCost > goldCollected || buildingData.woodCost > woodCollected ||
            buildingData.foodCost > foodCollected || buildingData.stoneCost > stoneCollected)
        {
            return false;
        }

        return true;
    }

    public bool CanQueueUnit(UnitData unitData)
    {
        // Make this unneccassary by having the queue button locked.
        TechNode node = faction.techTree.tree.Find(x => x.tech == unitData);
        
        if (!node.unlocked)
            return false;

        if (goldCollected < unitData.goldCost || woodCollected < unitData.woodCost ||
            foodCollected < unitData.foodCost || stoneCollected < unitData.stoneCost)
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

    public void DeductBuildingCost(BuildingData buildingData)
    {
        DeductResourcesFromStockpile(buildingData.goldCost, buildingData.foodCost, buildingData.woodCost, buildingData.stoneCost);
    }

    public void DeductUnitQueueCostFromStockpile(UnitData unitType)
    {
        woodCollected -= unitType.woodCost;
        foodCollected -= unitType.foodCost;
        goldCollected -= unitType.goldCost;
        stoneCollected -= unitType.stoneCost;

        UpdateWristDisplayResourceText();
    }
    public void OnDestroy()
    {
        CleanupEvents();
    }
}
