using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;
using Valve.VR;

public enum VillagerActorState { Idle, Lumberjacking, Farming, MiningGold, Transporting,
                                Building, Repairing, Attacking, Dying, MiningOre, Roaming, Gathering };



[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class VillagerActor : Actor
{
    [Header ("AI")]
    // Store previous state so villager can go back to work after attaching/fleeing
    [SerializeField] protected RTSUnitType rtsUnitType;
    public ResourceGatheringType wantedResourceType;
    [SerializeField] protected int cellSearchDistance = 20;
    [SerializeField] protected VillagerActorState previousState;
    [SerializeField] protected VillagerActorState currentState = VillagerActorState.Idle;
    [SerializeField] protected ResourceNode targetNode;
    [SerializeField] protected TerrainBuilding targetBuilding;
    [SerializeField] protected TerrainBuilding targetDamaged;

    [Header ("Stats")]
    public int carryingCapacity = 100;
    public float gatherCapacityPerSecond = 10;
    public float buildAndRepairCapacityPerSecond = 10;

    [Header("Animation")]
    [SerializeField] protected Animator animator;

    [Header ("Visuals")]
    public GameObject grainCargoDisplayObject;
    public GameObject woodCargoDisplayObject;
    public GameObject oreCargoDisplayObject;
    public GameObject goldCargoDisplayObject;
    public GameObject grainHandToolDisplayObject;
    public GameObject woodHandToolDisplayObject;
    public GameObject oreHandToolDisplayObject;
    public GameObject goldHandToolDisplayObject;
    public GameObject builderHandToolDisplayObject;

    [Header ("Audio")]
    public bool playRandomOnPickUpAudio;
    public AudioClip woodChoppingAudio;
    public AudioClip goldMiningAudio;
    public AudioClip oreMiningAudio;
    public AudioClip grainGatheringAudio;
    public AudioClip repairingAudio;
    private AudioSource audioSource;

    bool isHeld;
    public VillagerHoverMenu villagerHoverMenu;
    GameObject currentCargoDisplayObject;
    GameObject currentHandToolDisplayObject;
    public int currentCargoTotal;
    ResourceGatheringType lastWantedResoureType;

    public override void Initialize()
    {
        base.Initialize();

        //previouState = VillagerActorState.Idle;

        animator = GetComponent<Animator>();
        if (!animator)
            Debug.Log("No animator component found.");

        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
            Debug.Log("No audiosource component found.");

        // Initialize villager AI state, display objects, etc.
        SetUnitType(rtsUnitType);

        //PlayerManager.instance.AddToPopulation(rtsUnitType);
    }

    public void OnPickUp()
    {
        isHeld = true;
        Freeze();
        this.enabled = false;
        ResetAI();
        villagerHoverMenu.Show();

        if (playRandomOnPickUpAudio)
            PlayOnPickUpSound();

        animator.StopPlayback();
    }

    void PlayOnPickUpSound()
    {
        audioSource.clip = GameMaster.GetAudio("unitPickup").GetClip();
        audioSource.Play();
    }

    public void OnDetachFromHand()
    {
        isHeld = false;
        ResetAI();
        audioSource.Stop();
        villagerHoverMenu.Hide();
        animator.StartPlayback();
    }

    // This is is used to reenable the character after they have been
    // released from the hand AND after they have landed somewhere.
    private void OnCollisionEnter(Collision collision)
    {
        this.enabled = true;
        Unfreeze();
        //audioSource.Stop();

        ResourceNode node = collision.gameObject.GetComponent<ResourceNode>();
        if (node)
        {
            switch (node.type)
            {
                case ResourceGatheringType.Gold:
                {
                    targetNode = node;
                    currentState = VillagerActorState.MiningGold;
                    wantedResourceType = ResourceGatheringType.Gold;
                    ResetAI();
                    break;
                }

                case ResourceGatheringType.Grain:
                {
                    targetNode = node;
                    currentState = VillagerActorState.Farming;
                    wantedResourceType = ResourceGatheringType.Grain;
                    ResetAI();
                    break;
                }

                case ResourceGatheringType.Wood:
                {
                    targetNode = node;
                    currentState = VillagerActorState.Lumberjacking;
                    wantedResourceType = ResourceGatheringType.Wood;
                    ResetAI();
                    break;
                }

                case ResourceGatheringType.Stone:
                {
                    targetNode = node;
                    currentState = VillagerActorState.MiningOre;
                    wantedResourceType = ResourceGatheringType.Stone;
                    ResetAI();
                    break;
                }

                default:
                    break;
            }
            return;
        }

        // TerrainBuilding building = collision.gameObject.GetComponent<TerrainBuilding>();
        // if (building)
        // {
        //     if (building.NeedsRepair())
        //     {
        //         targetDamaged = building;
        //         currentState = VillagerActorState.Building;
        //         wantedResourceType = ResourceGatheringType.None;
        //         ResetPathing();
        //     }
        // }
    }

    public bool HasValidBuildOrRepairTarget()
    {
        return (targetDamaged != null);
    }

    public bool HasValidGatheringTarget()
    {
        return (targetNode != null);
    }

    public bool HasValidTransportTarget()
    {
        return (targetBuilding != null);
    }

    float timeElapsed;

    void Update()
    {
        if (isHeld)
            return;

        timeElapsed += Time.deltaTime;

        animator.SetBool("IsMoving", IsMoving());
        animator.SetInteger("VillagerActorState", (int)currentState);

        // if(timeElapsed < 1.5f)
        //     return;

        switch (currentState)
        {
            case VillagerActorState.Idle:
                break;

            case VillagerActorState.Building:
            case VillagerActorState.Repairing:
                if (!audioSource.isPlaying)
                {
                    audioSource.clip = repairingAudio;
                    audioSource.Play();
                }
                break;

            case VillagerActorState.Lumberjacking:
                audioSource.clip = GameMaster.GetAudio("chop_wood").GetClip();
                break;

            case VillagerActorState.MiningGold:
            case VillagerActorState.MiningOre:
                audioSource.clip = GameMaster.GetAudio("pickaxe").GetClip();
                break;

            case VillagerActorState.Farming:
                audioSource.clip = grainGatheringAudio;
                audioSource.Play();
                break;

            case VillagerActorState.Transporting:
            case VillagerActorState.Roaming:
                break;

            default:
                break;
        }

        timeElapsed = 0f;
    }

    public override void Tick()
    {
        if (isHeld)// || !StateChanged())
            return;

        switch (currentState)
        {
            case VillagerActorState.Building:
            case VillagerActorState.Repairing:
            {
                if ( HasValidBuildOrRepairTarget())
                {
                    Body body = targetDamaged.GetComponent<Body>();

                    //  Reached our target
                    if (Util.DistanceUnsquared(gridPosition, body.gridPosition) <= body.GetCellVolumeSqr())
                    {
                        LookAt(body.gridPosition.x, body.gridPosition.y);

                        // if (targetDamaged.NeedsRepair())
                        // {
                        //     int amountToRepair = (int)(buildAndRepairCapacityPerSecond / (60 / Constants.ACTOR_TICK_RATE));
                        //     targetDamaged.RepairDamage(amountToRepair);
                        // }
                        // else
                        // {
                        //     FindDamaged();
                        // }
                    }
                    else
                    {
                        //  Pathfind to the target
                        Goto( body.GetNearbyCoord() );
                        //animator.Play("Walk");
                    }
                }
                else
                {
                    FindDamaged();

                    //  If we can't find a resource, wander around
                    if ( !HasValidBuildOrRepairTarget() )
                    {
                        currentState = VillagerActorState.Roaming;
                        //animator.Play("Walk");
                        // Debug.Log(gameObject.name + " couldn't find " + currentGatheringResourceType + ", going to roam around now.");
                    }
                }
                break;
            }

            case VillagerActorState.Gathering:
            case VillagerActorState.Lumberjacking:
            case VillagerActorState.MiningGold:
            case VillagerActorState.MiningOre:
            case VillagerActorState.Farming:
            {
                if ( HasValidGatheringTarget())
                {
                    Body body = targetNode.GetComponent<Body>();

                    Debug.DrawRay(World.ToTransformSpace(body.gridPosition), Vector3.up, Color.red, 0.5f);

                    //  Reached our target
                    if (Util.DistanceUnsquared(gridPosition, body.gridPosition) <= body.GetCellVolumeSqr())
                    {

                        LookAt(body.gridPosition.x, body.gridPosition.y);

                        if (currentCargoTotal < carryingCapacity)
                        {
                            int amountToRemove = (int)(gatherCapacityPerSecond / (60 / Constants.ACTOR_TICK_RATE));
                            if (amountToRemove < 2)
                                amountToRemove = 2; // < 2 / (60/20) = .3333 which ends up as 0
                            amountToRemove = Mathf.Clamp( carryingCapacity - currentCargoTotal, 0, amountToRemove );
                            currentCargoTotal += amountToRemove;
                            targetNode.decreaseCurrentResourceAmount(amountToRemove);
                        }
                        else
                        {
                            currentCargoTotal = carryingCapacity;
                            currentState = VillagerActorState.Transporting;
                            DisplayCargo(true);
                            // Debug.Log(gameObject.name + " is done gathering and is now transporting " + currentCargo + " " + currentGatheringResourceType + ".");
                        }
                    }
                    else
                    {
                        //  Pathfind to the target
                        Goto( body.GetNearbyCoord() );
                        //animator.Play("Walk");
                    }
                }
                else
                {
                    FindResource();

                    //  If we can't find a resource, wander around
                    if ( !HasValidGatheringTarget() )
                    {
                        currentState = VillagerActorState.Roaming;
                        //animator.Play("Walk");
                        // Debug.Log(gameObject.name + " couldn't find " + currentGatheringResourceType + ", going to roam around now.");
                    }
                }
                break;
            }

            case VillagerActorState.Transporting:
            {
                if ( HasValidTransportTarget())
                {
                    Body body = targetBuilding.GetComponent<Body>();

                    Debug.DrawRay(World.ToTransformSpace(body.gridPosition), Vector3.up, Color.red, 0.5f);

                    if (Util.DistanceUnsquared(gridPosition, body.gridPosition) <= body.GetCellVolumeSqr())
                    {
                        //  Reached our target
                        // Debug.Log("Dropped off " + currentCargo + " " + currentGatheringResourceType + ".");
                        Valve.VR.InteractionSystem.Player.instance.GetComponent<PlayerManager>().AddResourceToStockpile(wantedResourceType, currentCargoTotal);
                        currentCargoTotal = 0;
                        DisplayCargo(false);

                        currentState = VillagerActorState.Gathering;
                    }
                    else
                    {
                        //  Pathfind to the target
                        Goto( body.GetNearbyCoord() );
                        // animator.Play("Walk");
                    }
                }
                else
                {
                    FindBuilding();

                    //  If we can't find a building, wander around
                    if ( !HasValidTransportTarget() )
                    {
                        currentState = VillagerActorState.Roaming;
                        //animator.Play("Walk");
                        // Debug.Log(gameObject.name + " couldn't find a building to drop off my cargo, going to roam around now.");
                    }
                }
                break;
            }

            case VillagerActorState.Roaming:
            {
                Goto(Random.Range(gridPosition.x - 4, gridPosition.x + 4),
                    Random.Range(gridPosition.x - 4, gridPosition.x + 4));
                //animator.Play("Walk");

                break;
            }

            default:
                break;
        }
    }

    bool StateChanged()
    {
        return currentState != previousState;
    }

    public void SetUnitType(RTSUnitType type)
    {
        if (currentHandToolDisplayObject)
            currentHandToolDisplayObject.SetActive(false);

        lastWantedResoureType = wantedResourceType;

        rtsUnitType = type;
        targetNode = null;
        targetBuilding = null;

        // Retarded, shouldn't be neccassary ... bit it is.
        if (!audioSource)
            audioSource = GetComponent<AudioSource>();

        switch ( rtsUnitType )
        {
            case RTSUnitType.Builder:
                {
                    currentState = VillagerActorState.Building;
                    wantedResourceType = ResourceGatheringType.None;
                    builderHandToolDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = builderHandToolDisplayObject;
                    audioSource.clip = GameMaster.GetAudio("builder").GetClip();

                    break;
                }

            case RTSUnitType.Farmer:
                {
                    currentState = VillagerActorState.Farming;
                    wantedResourceType = ResourceGatheringType.Grain;
                    //handGrainDisplayObject.SetActive(true);
                    currentHandToolDisplayObject = null;// handGrainDisplayObject;
                    audioSource.clip = GameMaster.GetAudio("farmer").GetClip();

                    break;
                }

            case RTSUnitType.Lumberjack:
            {
                currentState = VillagerActorState.Lumberjacking;
                wantedResourceType = ResourceGatheringType.Wood;
                woodHandToolDisplayObject.SetActive(true);
                currentHandToolDisplayObject = woodHandToolDisplayObject;
                audioSource.clip = GameMaster.GetAudio("lumberjack").GetClip();

                break;
            }

            case RTSUnitType.GoldMiner:
            {
                currentState = VillagerActorState.MiningGold;
                wantedResourceType = ResourceGatheringType.Gold;
                goldHandToolDisplayObject.SetActive(true);
                currentHandToolDisplayObject = goldHandToolDisplayObject;
                audioSource.clip = GameMaster.GetAudio("miner").GetClip();

                break;
            }

            case RTSUnitType.StoneMiner:
            {
                currentState = VillagerActorState.MiningOre;
                wantedResourceType = ResourceGatheringType.Stone;
                oreHandToolDisplayObject.SetActive(true);
                currentHandToolDisplayObject = oreHandToolDisplayObject;
                audioSource.clip = GameMaster.GetAudio("miner").GetClip();

                break;
            }
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void DisplayCargo(bool visible)
    {
        if (currentCargoDisplayObject)
            currentCargoDisplayObject.SetActive(false);

        switch (wantedResourceType)
        {
            case ResourceGatheringType.Grain:
            {
                grainCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = grainCargoDisplayObject;
                break;
            }

            case ResourceGatheringType.Wood:
            {
                woodCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = woodCargoDisplayObject;
                break;
            }

            case ResourceGatheringType.Stone:
            {
                oreCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = oreCargoDisplayObject;
                break;
            }

            case ResourceGatheringType.Gold:
            {
                goldCargoDisplayObject.SetActive(visible);
                currentCargoDisplayObject = goldCargoDisplayObject;
                break;
            }
        }
    }

    private void FindBuilding(List<TerrainBuilding> blacklist = null)
    {
        switch (wantedResourceType)
        {
            case ResourceGatheringType.Wood:
                FindLumberMills(blacklist);
                break;

            case ResourceGatheringType.Gold:
                FindTownHalls(blacklist);
                break;

            case ResourceGatheringType.Grain:
                FindGranaries(blacklist);
                break;

        }
    }

    private void FindDamaged(List<TerrainBuilding> blacklist = null)
    {
        TerrainBuilding nearestBuilding = null;

        //  Find the nearest townhall within range
        int shortestDistance = cellSearchDistance * cellSearchDistance; //  Square the distance
        foreach (TerrainBuilding damagedBuildings in ResourceManager.GetBuildAndRepairObjects())
        {
            if (damagedBuildings == null) continue;   //  TODO trim null values from resource manager
            if (blacklist != null && blacklist.Contains(damagedBuildings)) continue;

            Body body = damagedBuildings.GetComponent<Body>();
            int distance = (int)Util.DistanceUnsquared(gridPosition, body.gridPosition);

            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                nearestBuilding = damagedBuildings;
            }
        }

        if (nearestBuilding != null)
        {
            targetDamaged = nearestBuilding;
        }
    }

    private void FindGranaries(List<TerrainBuilding> blacklist = null)
    {
        TerrainBuilding nearestBuilding = null;

        //  Find the nearest townhall within range
        int shortestDistance = cellSearchDistance * cellSearchDistance; //  Square the distance
        foreach (TerrainBuilding granary in ResourceManager.GetGrainDropoffObjects())
        {
            if (granary == null) continue;   //  TODO trim null values from resource manager
            if (blacklist != null && blacklist.Contains(granary)) continue;

            Body body = granary.GetComponent<Body>();
            int distance = (int)Util.DistanceUnsquared(gridPosition, body.gridPosition);

            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                nearestBuilding = granary;
            }
        }

        if (nearestBuilding != null)
        {
            targetBuilding = nearestBuilding;
        }
    }

    private void FindTownHalls(List<TerrainBuilding> blacklist = null)
    {
        TerrainBuilding nearestBuilding = null;

        //  Find the nearest townhall within range
        int shortestDistance = cellSearchDistance * cellSearchDistance; //  Square the distance
        foreach (TerrainBuilding townhall in ResourceManager.GetGoldDroppoffObjects())
        {
            if (townhall == null) continue;   //  TODO trim null values from resource manager
            if (blacklist != null && blacklist.Contains(townhall)) continue;

            Body body = townhall.GetComponent<Body>();
            int distance = (int)Util.DistanceUnsquared(gridPosition, body.gridPosition);

            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                nearestBuilding = townhall;
            }
        }

        if (nearestBuilding != null)
        {
            targetBuilding = nearestBuilding;
        }
    }

    private void FindLumberMills(List<TerrainBuilding> blacklist = null)
    {
        TerrainBuilding nearestBuilding = null;

        //  Find the nearest lumbermill within range
        int shortestDistance = cellSearchDistance * cellSearchDistance; //  Square the distance
        foreach (TerrainBuilding lumbermill in ResourceManager.GetWoodDropoffObjects())
        {
            if (lumbermill == null) continue;   //  TODO trim null values from resource manager
            if (blacklist != null && blacklist.Contains(lumbermill)) continue;

            Body body = lumbermill.GetComponent<Body>();
            int distance = (int)Util.DistanceUnsquared(gridPosition, body.gridPosition);

            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                nearestBuilding = lumbermill;
            }
        }

        if (nearestBuilding != null)
        {
            targetBuilding = nearestBuilding;
        }
    }

    private void FindResource(List<ResourceNode> blacklist = null)
    {


        switch (wantedResourceType)
        {
            case ResourceGatheringType.Wood:
                FindWood(blacklist);
                break;

            case ResourceGatheringType.Gold:
                FindGold(blacklist);
                break;

            case ResourceGatheringType.Grain:
                FindGrain(blacklist);
                break;

        }
    }

    private void FindGrain(List<ResourceNode> blacklist = null)
    {
        ResourceNode nearestNode = null;

        //  Find the nearest tree within range
        int shortestDistance = cellSearchDistance * cellSearchDistance; //  Square the distance
        foreach (ResourceNode grain in ResourceManager.GetGrain())
        {
            if (grain == null) continue;   //  TODO trim null values from resource manager
            if (blacklist != null && blacklist.Contains(grain)) continue;

            Body body = grain.GetComponent<Body>();
            int distance = (int)Util.DistanceUnsquared(gridPosition, body.gridPosition);

            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                nearestNode = grain;
            }
        }

        if (nearestNode != null)
        {
            targetNode = nearestNode;
        }
    }

    private void FindWood(List<ResourceNode> blacklist = null)
    {
        ResourceNode nearestNode = null;

        //  Find the nearest tree within range
        int shortestDistance = cellSearchDistance * cellSearchDistance; //  Square the distance
        foreach (ResourceNode tree in ResourceManager.GetTrees())
        {
            if (tree == null) continue;   //  TODO trim null values from resource manager
            if (blacklist != null && blacklist.Contains(tree)) continue;

            Body body = tree.GetComponent<Body>();
            int distance = (int)Util.DistanceUnsquared(gridPosition, body.gridPosition);

            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                nearestNode = tree;
            }
        }

        if (nearestNode != null)
        {
            targetNode = nearestNode;
        }
    }
    private void FindGold(List<ResourceNode> blacklist = null)
    {
        ResourceNode nearestNode = null;

        //  Find the nearest gold within range
        int shortestDistance = cellSearchDistance * cellSearchDistance; //  Square the distance
        foreach (ResourceNode gold in ResourceManager.GetGold())
        {
            if (gold == null) continue;   //  TODO trim null values from resource manager
            if (blacklist != null && blacklist.Contains(gold)) continue;

            Body body = gold.GetComponent<Body>();
            int distance = (int)Util.DistanceUnsquared(gridPosition, body.gridPosition);

            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                nearestNode = gold;
            }
        }

        if (nearestNode != null)
        {
            targetNode = nearestNode;
        }
    }

}
