using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swordfish;
using Swordfish.Library.Collections;
using Swordfish.Library.Types;
using Swordfish.Navigation;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PlayerManager : MonoBehaviour
{

    public bool newTechTreeInstance;

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

    public GameObject mainMenuPrefab;
    private GameObject mainMenuObject;

    //=========================================================================
    [Header("Attachment/Tracking Points")]
    public Transform rHandAttachmentPoint;
    public Transform rHandTrackingPoint;
    public Transform lHandAttachmentPoint;
    public Transform lHandTrackingPoint;
    public GameObject autohideHandMenuObject;

    //=========================================================================
    [Header("Prefabs")]
    public GameObject handBuildMenuPrefab;
    public GameObject defaultButtonLockPrefab;
    public GameObject buttonLockEpoch2Prefab;
    public GameObject buttonLockEpoch3Prefab;
    public GameObject buttonLockEpoch4Prefab;
    public GameObject defaultSlotLockedPrefab;
    public GameObject slotLockEpoch2Prefab;
    public GameObject slotLockEpoch3Prefab;
    public GameObject slotLockEpoch4Prefab;

   
    private GripPan gripPan;
    private Hand buildMenuHand;
    private Hand selectionHand;

    private GameObject handBuildMenuGameObject;
    public GameObject HandBuildMenuGameObject { get => handBuildMenuGameObject; }

    protected BuildMenu buildMenu;
    public BuildMenu Buildmenu { get => buildMenu; }

    private Hand rightHand;
    private Hand leftHand;

    public event EventHandler<StoneResourceChangedEvent> OnStoneResourceChanged;
    public class StoneResourceChangedEvent : Swordfish.Event
    {
        public int amount;
        public int newValue;
    }

    public event EventHandler<GoldResourceChangedEvent> OnGoldResourceChanged;
    public class GoldResourceChangedEvent : Swordfish.Event
    {
        public int amount;
        public int newValue;
    }

    public event EventHandler<WoodResourceChangedEvent> OnWoodResourceChanged;
    public class WoodResourceChangedEvent : Swordfish.Event
    {
        public int amount;
        public int newValue;
    }

    public event EventHandler<FoodResourceChangedEvent> OnFoodResourceChanged;
    public class FoodResourceChangedEvent : Swordfish.Event
    {
        public int amount;
        public int newValue;
    }

    public event EventHandler<PopulationChangedEvent> OnPopulationChanged;
    public class PopulationChangedEvent : Swordfish.Event
    {
        public int civilianPopulation;
        public int militaryPopulation;
        public int totalPopulation;
    }

    public event EventHandler<PopulationLimitChangedEvent> OnPopulationLimitChanged;
    public class PopulationLimitChangedEvent : Swordfish.Event
    {
        public int totalPopulation;
        public int newValue;
    }

    public static event EventHandler<UnitAttributeBonusChangeEvent> OnUnitAttributeBonusChangedEvent;
    public class UnitAttributeBonusChangeEvent : Swordfish.Event
    {
        public UnitData unitData;
        public StatUpgradeContainer bonus;
    }

    public static event EventHandler<BuildingAttributeBonusChangeEvent> OnBuildingAttributeBonusChangedEvent;
    public class BuildingAttributeBonusChangeEvent : Swordfish.Event
    {
        public BuildingData buildingData;
        public StatUpgradeContainer bonus;
    }

    public static Dictionary<UnitData, List<StatUpgradeContainer>> AllUnitAttributeBonuses = new();
    public static Dictionary<BuildingData, List<StatUpgradeContainer>> AllBuildingAttributeBonuses = new();

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

    public void QuitGame()
    {
        Application.Quit();
    }

    void Start()
    {
        HookIntoEvents();

        headAudioSource.transform.SetParent(Player.instance.hmdTransform);
        headAudioSource.transform.localPosition = Vector3.zero;

        if (newTechTreeInstance && faction)
        {
            currentTree = Instantiate(faction.techTree);
            faction = Instantiate(faction);
            faction.techTree = currentTree;
            if (currentTree)
                Debug.Log("New tech tree instance instantiated.");
        }

        rightHand = Player.instance.rightHand;
        leftHand = Player.instance.leftHand;

        if (!autohideHandMenuObject)
            Debug.Log("autohideHandMenuObject not set.", this);

        // Initialize hammer position
        leftHandHammerStorage.gameObject.SetActive(hammerOnLeft);
        rightHandHammerStorage.gameObject.SetActive(hammerOnRight);

        InitializeHandBuildMenu();

        handMenuToggle?.AddOnStateDownListener(OnHandToggleMenuRightDown, SteamVR_Input_Sources.RightHand);
        handMenuToggle?.AddOnStateDownListener(OnHandToggleMenuLeftDown, SteamVR_Input_Sources.LeftHand);

        faction.techTree.RefreshNodes();

        // Initialize and activate the menu so it can catch startup resource values. Resource
        // displays on the main menu hook into events on PlayerManager so we initialize
        // resources after the menu has been loaded.
        mainMenuObject = Instantiate(mainMenuPrefab, Vector3.zero, Quaternion.identity);
        mainMenuObject.SetActive(true);

        InitializeResources();

        mainMenuObject.SetActive(false);

        Valve.VR.OpenVR.Chaperone?.ResetZeroPose(ETrackingUniverseOrigin.TrackingUniverseStanding);
    }

    public void ToggleMainMenu()
    {
        if (mainMenuObject.activeSelf)
            CloseMenu();
        else
            ShowMenu();
    }
    public void ShowMenu()
    {
        float menuOffset = 0.75f;
        Vector3 hmdPos = Player.instance.hmdTransform.position;
        Vector3 hmdDirection = Player.instance.hmdTransform.forward;
        Vector3 spawnPos = hmdPos + hmdDirection * menuOffset;

        mainMenuObject.transform.position = spawnPos;
        mainMenuObject.transform.rotation = new Quaternion(0.0f, Player.instance.hmdTransform.rotation.y, 0.0f, Player.instance.hmdTransform.rotation.w);
        mainMenuObject.SetActive(true);
    }

    public void CloseMenu()
    {
        mainMenuObject.SetActive(false);
    }

    void OnDestroy()
    {
        CleanupEvents();
    }

    public void PlayEpochResearchCompleteAudio()
    {
        PlayAudioAtHeadSource(GameMaster.Instance.epochResearchCompleteSound);
    }

    public void PlayBuildingPlacedSound()
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

    public void AddBuildingStatUpgrade(BuildingData buildingData, StatUpgradeContainer attributeBonus)
    {
        if (!AllBuildingAttributeBonuses.ContainsKey(buildingData))
        {
            List<StatUpgradeContainer> newBonusList = new()
            {
                attributeBonus
            };

            AllBuildingAttributeBonuses.Add(buildingData, newBonusList);
        }
        else
        {
            StatUpgradeContainer foundAttributeBonus = AllBuildingAttributeBonuses[buildingData].Find(x => x.targetAttribute == attributeBonus.targetAttribute);
            if (foundAttributeBonus == null)
                AllBuildingAttributeBonuses[buildingData].Add(attributeBonus);
            else if (foundAttributeBonus != null)
                foundAttributeBonus.amount += attributeBonus.amount;
        }

        BuildingAttributeBonusChangeEvent e = new()
        {
            buildingData = buildingData,
            bonus = attributeBonus
        };

        OnBuildingAttributeBonusChangedEvent?.Invoke(this, e);
    }

    public void AddUnitStatUpgrade(UnitData unitData, StatUpgradeContainer attributeBonus)
    {
        if (!AllUnitAttributeBonuses.ContainsKey(unitData))
        {
            List<StatUpgradeContainer> newBonusList = new()
            {
                new StatUpgradeContainer(attributeBonus)
            };

            AllUnitAttributeBonuses.Add(unitData, newBonusList);
        }
        else
        {
            StatUpgradeContainer foundAttributeBonus = AllUnitAttributeBonuses[unitData].Find(x => x.targetAttribute == attributeBonus.targetAttribute);
            if (foundAttributeBonus == null)
                AllUnitAttributeBonuses[unitData].Add(new StatUpgradeContainer(attributeBonus));
            else if (foundAttributeBonus != null)
                foundAttributeBonus.amount += attributeBonus.amount;
        }

        UnitAttributeBonusChangeEvent e = new()
        {
            unitData = unitData,
            bonus = attributeBonus
        };

        OnUnitAttributeBonusChangedEvent?.Invoke(this, e);
        //Debug.LogFormat("+{0} {1} bonus added for {2}.", attributeBonus.amount, attributeBonus.targetAttribute.ToString(), unitData.title);
    }

    public GameObject GetButtonLockPrefab(TechBase tech)
    {
        GameObject lockPrefab = null;
        TechNode node = faction.techTree.FindNode(tech);
        lockPrefab = defaultButtonLockPrefab;

        if (node != null)
        {
            foreach (TechBase techBase in node.techRequirements)
            {
                if (techBase is EpochUpgrade)
                {
                    int id = ((EpochUpgrade)techBase).epochId;
                    if (id <= 1)
                        continue;

                    switch (id)
                    {
                        case 2:
                            lockPrefab = buttonLockEpoch2Prefab;
                            break;
                        case 3:
                            lockPrefab = buttonLockEpoch3Prefab;
                            break;
                        case 4:
                            lockPrefab = buttonLockEpoch4Prefab;
                            break;
                    }

                    break;
                }
            }
        }
        return lockPrefab;
    }

    public GameObject GetSlotLockPrefab(TechBase tech)
    {
        GameObject lockPrefab = null;
        TechNode node = faction.techTree.FindNode(tech);
        lockPrefab = defaultSlotLockedPrefab;

        if (node != null)
        {
            foreach (TechBase techBase in node.techRequirements)
            {
                if (techBase is EpochUpgrade)
                {
                    int id = ((EpochUpgrade)techBase).epochId;
                    if (id <= 1)
                        continue;

                    switch (id)
                    {
                        case 2:
                            lockPrefab = slotLockEpoch2Prefab;
                            break;
                        case 3:
                            lockPrefab = slotLockEpoch3Prefab;
                            break;
                        case 4:
                            lockPrefab = slotLockEpoch4Prefab;
                            break;
                    }

                    break;
                }
            }
        }
        return lockPrefab;
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
        foreach (Structure st in Structure.AllStructures)
        {
            if (st == structure)
                continue;

            if (st.buildingData.title == structure.buildingData.title)
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
            Vector3 position = new(14.8597126f, 0.681457937f, -10.2036371f);
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

    public void AddToPopulation(UnitV2 unit)
    {
        // Determine if the unit should be added to civilian or military population
        if (unit.IsCivilian)
            civilianPopulation += unit.unitData.populationCost;
        else
            militaryPopulation += unit.unitData.populationCost;

        totalPopulation += unit.unitData.populationCost;
        queueCount -= unit.unitData.populationCost;
        if (queueCount < 0) queueCount = 0;

        PopulationChangedEvent e = new()
        {
            civilianPopulation = civilianPopulation,
            militaryPopulation = militaryPopulation,
            totalPopulation = totalPopulation,
        };
        OnPopulationChanged?.Invoke(this, e);
    }

    public void RemoveFromPopulation(UnitV2 unit)
    {
        // Determine if the unit should be removed from civilian or military population
        if (unit.IsCivilian)
            civilianPopulation -= unit.unitData.populationCost;
        else
            militaryPopulation -= unit.unitData.populationCost;

        totalPopulation -= unit.unitData.populationCost;
        PopulationChangedEvent e = new()
        {
            civilianPopulation = civilianPopulation,
            militaryPopulation = militaryPopulation,
            totalPopulation = totalPopulation,
        };
        OnPopulationChanged?.Invoke(this, e);
    }

    public void IncreasePopulationLimit(int amountToIncreaseBy)
    {
        populationLimit += amountToIncreaseBy;

        PopulationLimitChangedEvent e = new()
        {
            totalPopulation = totalPopulation,
            newValue = populationLimit,
        };
        OnPopulationLimitChanged?.Invoke(this, e);
    }

    public void RemoveFromQueueCount(int amount = 1)
    {
        queueCount -= amount;
    }

    public void DecreasePopulationLimit(int amountDecreaseBy)
    {
        populationLimit -= amountDecreaseBy;
        PopulationLimitChangedEvent e = new()
        {
            totalPopulation = totalPopulation,
            newValue = populationLimit,
        };
        OnPopulationLimitChanged?.Invoke(this, e);
    }

    public void AddResourceToStockpile(ResourceGatheringType type, int amount)
    {

        switch (type)
        {
            case ResourceGatheringType.Wood:
                UpdateWoodResource(amount);
                break;

            case ResourceGatheringType.Meat:
            case ResourceGatheringType.Fish:
            case ResourceGatheringType.Grain:
            case ResourceGatheringType.Berries:
                UpdateFoodResource(amount);
                break;

            case ResourceGatheringType.Gold:
                UpdateGoldResource(amount);
                break;

            case ResourceGatheringType.Stone:
                UpdateStoneResource(amount);
                break;

            default:
                break;
        }

        faction.techTree.RefreshNodes();
    }

    private void UpdateStoneResource(int amount)
    {
        stoneCollected += amount;

        StoneResourceChangedEvent e = new()
        {
            amount = amount,
            newValue = stoneCollected,
        };
        OnStoneResourceChanged?.Invoke(this, e);
    }

    private void UpdateGoldResource(int amount)
    {
        goldCollected += amount;

        GoldResourceChangedEvent e = new()
        {
            amount = amount,
            newValue = goldCollected,
        };
        OnGoldResourceChanged?.Invoke(this, e);
    }

    private void UpdateWoodResource(int amount)
    {
        woodCollected += amount;

        WoodResourceChangedEvent e = new()
        {
            amount = amount,
            newValue = woodCollected,
        };
        OnWoodResourceChanged?.Invoke(this, e);
    }

    private void UpdateFoodResource(int amount)
    {
        foodCollected += amount;

        FoodResourceChangedEvent e = new()
        {
            amount = amount,
            newValue = foodCollected,
        };
        OnFoodResourceChanged?.Invoke(this, e);
    }

    private void InitializeResources()
    {
        UpdateFoodResource(0);
        UpdateGoldResource(0);
        UpdateStoneResource(0);
        UpdateWoodResource(0);
    }

    public void DeductTechResourceCost(TechBase techBase)
    {
        UpdateGoldResource(-techBase.goldCost);
        UpdateFoodResource(-techBase.foodCost);
        UpdateStoneResource(-techBase.stoneCost);
        UpdateWoodResource(-techBase.woodCost);

        // TODO: Switch this to events?
        faction.techTree.RefreshNodes();
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
