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

#if UNITY_EDITOR
    public bool newTechTreeInstance;
#endif

    public TechTree currentTree;
    //=========================================================================
    [Header("Stats/Resources")]
    public int woodCollected;
    public int goldCollected;
    public int stoneCollected;
    public int foodCollected;
    public int civilianPopulation;
    public int militaryPopulation;
    public int totalPopulation;
    public int populationLimit;
    public int queueCount;
    public Faction faction;

    //=========================================================================
    [Header("Audio Sources")]
    public AudioSource headAudioSource;

    [Header("UI")]
    public SteamVR_Action_Boolean handMenuToggle;

    //=========================================================================
    [Header("Hammer")]
    public bool hammerOnLeft = true;
    public bool hammerOnRight = false;
    public Transform rightHandHammerStorage;
    public Transform leftHandHammerStorage;

    //=========================================================================
    [Header("Autohide Hand Menu")]
    [Tooltip("Switch between clipboard build menu or palm build menu.")]
    public bool autoHideHandMenuEnabled;
    public float handMenuTrackingSensitivity = 0.5f;


    //=========================================================================
    [Header("Attachment/Tracking Points")]
    public Transform rHandAttachmentPoint;
    public Transform rHandTrackingPoint;
    public Transform lHandAttachmentPoint;
    public Transform lHandTrackingPoint;
    public GameObject autohideHandMenuObject;

    //=========================================================================
    [Header("Information Displays")]
    public WristDisplay WristDisplay;
    public WristDisplay FaceDisplay;

    //=========================================================================
    [Header("Prefabs")]
    public GameObject handBuildMenuPrefab;

    private GripPan gripPan;
    private Hand buildMenuHand;
    private Hand selectionHand;
    
    private GameObject handBuildMenuGameObject;
    public GameObject HandBuildMenuGameObject{ get => handBuildMenuGameObject; }

    protected BuildMenu buildMenu;
    public BuildMenu Buildmenu { get => buildMenu; }

    private Hand rightHand;
    private Hand leftHand;


    private static PlayerManager _instance;
    //private bool isAutohideHandMenuVisible;
    
    public static PlayerManager Instance
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

        headAudioSource.transform.SetParent(Player.instance.hmdTransform);
        headAudioSource.transform.localPosition = Vector3.zero;

#if !UNITY_EDITOR
        TechTree tree = Instantiate(faction.techTree);
        faction = Instantiate(faction);
        faction.techTree = tree;
#else
        if (newTechTreeInstance)
        {
            TechTree tree = Instantiate(faction.techTree);
            faction = Instantiate(faction);
            faction.techTree = tree;
            currentTree = faction.techTree;
        }
#endif
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
        leftHandHammerStorage.gameObject.SetActive(hammerOnLeft);
        rightHandHammerStorage.gameObject.SetActive(hammerOnRight);

        InitializeHandBuildMenu();        

        handMenuToggle?.AddOnStateDownListener(OnHandToggleMenuRightDown, SteamVR_Input_Sources.RightHand);
        handMenuToggle?.AddOnStateDownListener(OnHandToggleMenuLeftDown, SteamVR_Input_Sources.LeftHand);

        faction.techTree.RefreshNodes();

        Valve.VR.OpenVR.Chaperone?.ResetZeroPose(ETrackingUniverseOrigin.TrackingUniverseStanding);
    }

    void OnDestroy()
    {
        CleanupEvents();
    }

    public void PlayEpochResearchCompleteAudio()
    {
        PlayAudioAtHeadSource(GameMaster.Instance.epochResearchCompleteSound);
    }

    public void PlayBuildingPlacementAllowedAudio()
    {
        PlayAudioAtHeadSource(GameMaster.Instance.buildingPlacementAllowedSound);
    }

    public void PlayBuildingPlacementDeniedAudio()
    {
        PlayAudioAtHeadSource(GameMaster.Instance.buildingPlacementDeniedSound);
    }

    public void PlaySetRallyPointSound()
    {
        PlayAudioAtHeadSource(GameMaster.Instance.setRallyPointSound);
    }

    public void PlayTeleportSound()
    {
        PlayAudioAtHeadSource(GameMaster.Instance.teleportSound);
    }

    public void PlayQueueButtonDownSound()
    {
        PlayAudioAtHeadSource(GameMaster.Instance.onQueueButtonDownSound);
    }

    public void PlayQueueButtonUpSound()
    {
        PlayAudioAtHeadSource(GameMaster.Instance.onQueueButtonUpSound);
    }

    public void PlayAudioAtHeadSource(AudioClip clip)
    {
        PlayAudioClip(headAudioSource, clip);
    }

    //=========================================================================
    private void PlayAudioClip(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.Play();
    }

    void Update()
    {
        if (autoHideHandMenuEnabled)
        {
            if (lHandTrackingPoint.right.y > handMenuTrackingSensitivity)
            {
                //buildMenu.RefreshSlots();
                SetLeftHandInteraction(false);
                //isAutohideHandMenuVisible = true;
                autohideHandMenuObject.SetActive(true);
            }
            else
            {
                autohideHandMenuObject.SetActive(false);
                SetLeftHandInteraction(true);
                //isAutohideHandMenuVisible = false;
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
    
    public void ProcessRotateClockwiseEvent(Hand hand)
    {
        if (hand.currentAttachedObject == HandBuildMenuGameObject)
        {
            buildMenu.NextTab();

            // Refresh tech tree so events are called on previously
            // disabled build menu tab
            faction.techTree.RefreshNodes();
        }
    }

    public void ProcessRotateCounterClockwiseEvent(Hand hand) 
    {
        if (hand.currentAttachedObject == HandBuildMenuGameObject)
        {
            buildMenu.PreviousTab();

            // Refresh tech tree so events are called on previously
            // disabled build menu tab
            faction.techTree.RefreshNodes();
        }
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

                    // Set this building to built in tech tree
                    faction.techTree.SetIsBuilt(structure.buildingData);
                    break;
                case UnitV2 unit:
                    AddToPopulation(unit);
                    break;
            }
        }
    }

    private bool AnotherBuildingLikeThisExists(Structure structure)
    {
        foreach(Structure st in Structure.AllStructures)
        {
            if (st == structure)
                continue;

            if (st.buildingData.buildingType == structure.buildingData.buildingType)
                return true;
        }
        
        return false;
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

                    // Revoke isBuilt status if no instances of this building type exist
                    if (!AnotherBuildingLikeThisExists(structure))
                        faction.techTree.RevokeIsBuilt(structure.buildingData);
                    break;
                case UnitV2 unit:
                    RemoveFromPopulation(unit);
                    break;
            }
        }
    }

    private void InitializeHandBuildMenu()
    {
        if (handBuildMenuPrefab)
        {
            Vector3 position = new Vector3(14.8597126f, 0.681457937f, -10.2036371f);
            handBuildMenuGameObject = Instantiate(handBuildMenuPrefab, position, Quaternion.identity, autohideHandMenuObject.transform);
            buildMenu = handBuildMenuGameObject.GetComponent<BuildMenu>();
            handBuildMenuGameObject.SetActive(false);
        }

#if UNITY_EDITOR
        else
            Debug.LogError("HandBuildMenuPrefab not set.", this);
#endif

    }

    public void OnHandToggleMenuRightDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        //buildMenu.RefreshSlots();

        // Don't use both methods to display the menu.
        if (autoHideHandMenuEnabled) return;

        // Menu is already visible.
        if (handBuildMenuGameObject.activeSelf)
        {
            // Menu is attached to right hand, deactivate it, detach it, and
            // enable interactions in right hand.
            if (rightHand.currentAttachedObject == handBuildMenuGameObject)
            {
                rightHand.DetachObject(handBuildMenuGameObject);

                // TODO: This should be taken care of by DetachObject -> OnDetachedFromHand event 
                // but OnDetachedFromHand has no reciever (Interactable) for some reason.
                if (rightHand.skeleton != null)
                    rightHand.skeleton.BlendToSkeleton(0.2f);

                handBuildMenuGameObject.SetActive(false);
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
                rightHand.AttachObject(handBuildMenuGameObject, GrabTypes.Scripted);
                SetLeftHandInteraction(true);
            }
        }
        // Menu is not visible.
        else
        {
            // Disable interaction in right hand, activate the build menu, and 
            // attach build menu to right hand,
            SetRightHandInteraction(false);
            handBuildMenuGameObject.SetActive(true);
            rightHand.AttachObject(handBuildMenuGameObject, GrabTypes.Scripted);
        }

    }

    public void OnHandToggleMenuLeftDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        //buildMenu.RefreshSlots();

        // Don't use both methods to display the menu at the same time.
        if (autoHideHandMenuEnabled) return;

        // Menu is already visible.
        if (handBuildMenuGameObject.activeSelf)
        {
            // Menu is attached to left hand, deactivate it, detach it, and
            // enable interactions in left hand.
            if (leftHand.currentAttachedObject == handBuildMenuGameObject)
            {
                leftHand.DetachObject(handBuildMenuGameObject);

                // TODO: This should be taken care of by DetachObject -> OnDetachedFromHand event 
                // but OnDetachedFromHand has no reciever (Interactable) for some reason.
                if (leftHand.skeleton != null)
                    leftHand.skeleton.BlendToSkeleton(0.2f);

                handBuildMenuGameObject.SetActive(false);
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
                leftHand.AttachObject(handBuildMenuGameObject, GrabTypes.Scripted);
                SetRightHandInteraction(true);
            }
        }
        // Menu is not visible.
        else
        {
            // Disable interaction in left hand, activate the build menu, and 
            // attach build menu to left hand,
            SetLeftHandInteraction(false);
            handBuildMenuGameObject.SetActive(true);
            leftHand.AttachObject(handBuildMenuGameObject, GrabTypes.Scripted);
        }

    }
    
    public void ProcessTechQueueComplete(TechBase tech)
    {
        if (!faction.techTree.ResearchTech(tech))
            return;

        switch (tech)
        {
            case EpochUpgrade:
                PlaySetRallyPointSound();
                break;

            case TechResearcher:
                PlayEpochResearchCompleteAudio();
                break;

            case UnitData:
            case BuildingData:
                break;
        }        
        
        Debug.LogFormat("{0} research complete.", tech.title, this);
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

        faction.techTree.RefreshNodes();
        //buildMenu.RefreshSlots();
    }

    public void DeductTechResourceCost(TechBase techBase)
    {
        goldCollected -= techBase.goldCost;
        foodCollected -= techBase.foodCost;
        woodCollected -= techBase.woodCost;
        stoneCollected -= techBase.stoneCost;

        UpdateWristDisplayResourceText();

        // TODO: Switch this to events?
        faction.techTree.RefreshNodes();
        //buildMenu.RefreshSlots();
    }


    public bool CanAffordTech(TechBase techBase)
    {
        if (techBase.goldCost > goldCollected || techBase.woodCost > woodCollected ||
            techBase.foodCost > foodCollected || techBase.stoneCost > stoneCollected)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// This should be the last test called right before a queue is cleared
    /// or spawned.
    /// </summary>
    public bool TryToQueueTech(TechBase techBase)
    {
        // This should already be cleared by the enabling of the button in the techtree
        // but check just in case something changed since the last tree update.
        if (!CanAffordTech(techBase))
            return false;        

        if (techBase.populationCost > 0)
        {
            if (techBase.populationCost + totalPopulation + queueCount > populationLimit)
                return false;

            queueCount += techBase.populationCost;
            DeductTechResourceCost(techBase);
        }

        return true;
    }    
}
