using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;
using Valve.VR;

public enum VillagerActorState { Idle, Gathering, Transporting, Building, Repairing, Roaming };
public enum ResourceGatheringType { None, Grain, Wood, Ore, Gold };

public class VillagerActor : Actor
{
    [SerializeField] protected int cellSearchDistance = 20;
    [SerializeField] protected ResourceNode targetNode;
    [SerializeField] protected TerrainBuilding targetBuilding;
    public int carryingCapacity = 100;
    public float gatheringCapacityPerSecond = 10;
    public int currentCargo;
    public VillagerActorState currentState = VillagerActorState.Idle;

    // Store previous state so villager can go back to work after attaching/fleeing
    private VillagerActorState previouState;
    bool isHeld;

    public GameObject cargoGrainDisplayObject;
    public GameObject cargoWoodDisplayObject;
    public GameObject cargoOreDisplayObject;
    public GameObject cargoGoldDisplayObject;
    public GameObject handGrainDisplayObject;
    public GameObject handWoodDisplayObject;
    public GameObject handOreDisplayObject;
    public GameObject handGoldDisplayObject;
    GameObject currentCargoDisplayObject;
    GameObject currentHandDisplayObject;

    public ResourceGatheringType currentGatheringResourceType;
    ResourceGatheringType lastGatheringResoureType;

    public bool playAudio;

    public void Update()
    {
        if (lastGatheringResoureType == currentGatheringResourceType)
            return;

        if (currentHandDisplayObject)
            currentHandDisplayObject.SetActive(false);

        lastGatheringResoureType = currentGatheringResourceType;

        switch (currentGatheringResourceType)
        {
            case ResourceGatheringType.Grain:
            {
                //handGrainDisplayObject.SetActive(true);
                currentHandDisplayObject = null;// handGrainDisplayObject;
                break;
            }

            case ResourceGatheringType.Wood:
            {
                handWoodDisplayObject.SetActive(true);
                currentHandDisplayObject = handWoodDisplayObject;
                break;
            }

            case ResourceGatheringType.Ore:
            {
                handOreDisplayObject.SetActive(true);
                currentHandDisplayObject = handOreDisplayObject;
                break;
            }

            case ResourceGatheringType.Gold:
            {
                handGoldDisplayObject.SetActive(true);
                currentHandDisplayObject = handGoldDisplayObject;
                break;
            }
        }
    }
    public void OnPickUp()
    {
        isHeld = true;
        if (playAudio)
            PlaySound();
        Freeze();
        this.enabled = false;
        ResetPathing();
    }

    void PlaySound()
    {
        AudioSource audio = gameObject.GetComponent<AudioSource>();
        audio.clip = GameMaster.GetAudio("unitPickup").GetClip();
        audio.Play();
    }

    // public AudioClip[] otherClip; //make an arrayed variable (so you can attach more than one sound)

    // // Play random sound from variable
    // void PlaySound()
    // {
    //             //Assign random sound from variable
    //             audio.clip = otherClip[Random.Range(0,otherClip.length)];

    //     audio.Play();

    //     // Wait for the audio to have finished
    //     yield WaitForSeconds (audio.clip.length);

    //     //Now you should re-loop this function Like
    //             PlaySound();
    // }

    public void OnDetachFromHand()
    {
        isHeld = false;
        ResetPathing();
        //AudioSource audio = gameObject.GetComponent<AudioSource>();
        // No stopping the audio!
        //audio.Stop();
        //this.enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        this.enabled = true;
        Unfreeze();
        //AudioSource audio = gameObject.GetComponent<AudioSource>();
        //audio.Stop();
    }

    public bool HasValidGatheringTarget()
    {
        return (targetNode != null);
    }

    public bool HasValidTransportTarget()
    {
        return (targetBuilding != null);
    }

    public override void Tick()
    {
        if (isHeld)
            return;

        switch (currentState)
        {
            case VillagerActorState.Idle:
            {
                // Play idle animation
                GetComponentInChildren<Animator>().Play("Idle");
                break;
            }

            case VillagerActorState.Gathering:
            {
                if ( HasValidGatheringTarget())
                {
                    Body body = targetNode.GetComponent<Body>();

                    Debug.DrawRay(World.ToTransformSpace(body.gridPosition), Vector3.up, Color.red, 0.5f);

                    //  Reached our target
                    if (Util.DistanceUnsquared(gridPosition, body.gridPosition) <= body.GetCellVolumeSqr())
                    {
                        GetComponentInChildren<Animator>().Play("Attack", -1, 0f);

                        if (currentCargo < carryingCapacity)
                        {
                            int amountToRemove = (int)(gatheringCapacityPerSecond / (60 / Constants.ACTOR_TICK_RATE));
                            amountToRemove = Mathf.Clamp( carryingCapacity - currentCargo, 0, amountToRemove );
                            currentCargo += amountToRemove;
                            targetNode.decreaseCurrentResourceAmount(amountToRemove);
                        }
                        else
                        {
                            currentCargo = carryingCapacity;
                            currentState = VillagerActorState.Transporting;
                            DisplayCargo(true);
                            // Debug.Log(gameObject.name + " is done gathering and is now transporting " + currentCargo + " " + currentGatheringResourceType + ".");
                        }
                    }
                    else
                    {
                        //  Pathfind to the target
                        Goto( body.GetNearbyCoord() );
                        GetComponentInChildren<Animator>().Play("Walk");
                    }
                }
                else
                {
                    FindResource();

                    //  If we can't find a resource, wander around
                    if ( !HasValidGatheringTarget() )
                    {
                        currentState = VillagerActorState.Roaming;
                        GetComponentInChildren<Animator>().Play("Walk");
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
                        Valve.VR.InteractionSystem.Player.instance.GetComponent<PlayerManager>().AddResourceToStockpile(currentGatheringResourceType, currentCargo);
                        currentCargo = 0;
                        DisplayCargo(false);
                        currentState = VillagerActorState.Gathering;
                    }
                    else
                    {
                        //  Pathfind to the target
                        Goto( body.GetNearbyCoord() );
                        GetComponentInChildren<Animator>().Play("Walk");
                    }
                }
                else
                {
                    FindBuilding();

                    //  If we can't find a building, wander around
                    if ( !HasValidTransportTarget() )
                    {
                        currentState = VillagerActorState.Roaming;
                        // Debug.Log(gameObject.name + " couldn't find a building to drop off my cargo, going to roam around now.");
                    }
                }
                break;
            }

            case VillagerActorState.Roaming:
            {
                Goto(Random.Range(gridPosition.x - 4, gridPosition.x + 4),
                    Random.Range(gridPosition.x - 4, gridPosition.x + 4));

                break;
            }

            default:
                break;
        }
    }

    private void DisplayCargo(bool visible)
    {
        if (currentCargoDisplayObject)
            currentCargoDisplayObject.SetActive(false);

        switch (currentGatheringResourceType)
        {
            case ResourceGatheringType.Grain:
            {
                cargoGrainDisplayObject.SetActive(visible);
                currentCargoDisplayObject = cargoGrainDisplayObject;
                break;
            }

            case ResourceGatheringType.Wood:
            {
                cargoWoodDisplayObject.SetActive(visible);
                currentCargoDisplayObject = cargoWoodDisplayObject;
                break;
            }

            case ResourceGatheringType.Ore:
            {
                cargoOreDisplayObject.SetActive(visible);
                currentCargoDisplayObject = cargoOreDisplayObject;
                break;
            }

            case ResourceGatheringType.Gold:
            {
                cargoGoldDisplayObject.SetActive(visible);
                currentCargoDisplayObject = cargoGoldDisplayObject;
                break;
            }
        }
    }

    private void FindBuilding(List<TerrainBuilding> blacklist = null)
    {
        switch (currentGatheringResourceType)
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

    private void FindGranaries(List<TerrainBuilding> blacklist = null)
    {
        TerrainBuilding nearestBuilding = null;

        //  Find the nearest townhall within range
        int shortestDistance = cellSearchDistance * cellSearchDistance; //  Square the distance
        foreach (TerrainBuilding granary in ResourceManager.GetGranaries())
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
        foreach (TerrainBuilding townhall in ResourceManager.GetTownHalls())
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
        foreach (TerrainBuilding lumbermill in ResourceManager.GetLumberMills())
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


        switch (currentGatheringResourceType)
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
