using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;
using Valve.VR;

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

    public enum VillagerActorState { Idle, Gathering, Transporting, Fleeing, Repairing, Attacking, Roaming };

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
        switch (currentState)
        {
            case VillagerActorState.Idle:
            {
                // Play idle animation
                GetComponent<Animator>().Play("Idle");
                break;
            }
            
            case VillagerActorState.Gathering:
            {
                if ( HasValidGatheringTarget())
                {
                    Body body = targetNode.GetComponent<Body>();
                    
                    //  Reached our target
                    if (Util.DistanceUnsquared(gridPosition, body.gridPosition) <= 1.5f)
                    {               
                        GetComponent<Animator>().Play("Attack", -1, 0f); 
                                                        
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
                            Debug.Log(gameObject.name + " is done gathering and is now transporting " + currentCargo + " wood.");
                        }
                        Debug.DrawRay(targetNode.transform.position, Vector3.up, Color.red, 0.5f);
                    }
                    else
                    {
                        //  Pathfind to the target
                        Goto(body.gridPosition.x + Random.Range(-1, 1), body.gridPosition.y + Random.Range(-1, 1));
                        GetComponent<Animator>().Play("Walk");
                    }
                }
                else
                {
                    FindResource();

                    //  If we can't find a resource, wander around
                    if ( !HasValidGatheringTarget() )
                    {
                        currentState = VillagerActorState.Roaming;         
                        GetComponent<Animator>().Play("Walk");
                        Debug.Log(gameObject.name + " couldn't find wood, going to roam around now.");               
                    }    
                }
                break;
            }

            case VillagerActorState.Transporting:
            {
                if ( HasValidTransportTarget())
                {
                    Body body = targetBuilding.GetComponent<Body>();

                    if (Util.DistanceUnsquared(gridPosition, body.gridPosition) <= 1.5f)
                    {
                        //  Reached our target                        
                        Debug.Log("Dropped off " + currentCargo + " wood.");
                        Valve.VR.InteractionSystem.Player.instance.GetComponent<PlayerManager>().AddWoodToResources(currentCargo);
                        currentCargo = 0;
                        currentState = VillagerActorState.Gathering;
                        Debug.DrawRay(targetNode.transform.position, Vector3.up, Color.red, 0.5f);
                    }
                    else
                    {
                        //  Pathfind to the target
                        Goto(body.gridPosition.x + Random.Range(-1, 1), body.gridPosition.y + Random.Range(-1, 1));
                        GetComponent<Animator>().Play("Walk");
                    }
                }
                else
                {
                    FindBuilding();

                    //  If we can't find a building, wander around
                    if ( !HasValidTransportTarget() )
                    {
                        currentState = VillagerActorState.Roaming;                        
                        Debug.Log(gameObject.name + " couldn't find a Lumbermill to drop of my cargo, going to roam around now.");
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

        //  If we aren't pathfinding...
        // if ( !HasValidPath() )
        // {
            //  Pathfind to our target if we have one and havent reached it
            // if ( HasValidTarget())
            // {
            //     Body body = targetNode.GetComponent<Body>();

            //     if (Util.DistanceUnsquared(gridPosition, body.gridPosition) <= 1.5f)
            //     {
            //         //  Reached our target
            //         if (currentCargo < carryingCapacity)
            //         {
            //             targetNode.decreaseCurrentResourceAmount(gatheringCapacityPerSecond / Time.deltaTime);
            //         }
            //         else
            //         {
            //             currentCargo = carryingCapacity;

            //         }

            //         Debug.DrawRay(targetNode.transform.position, Vector3.up, Color.red, 0.5f);
            //     }
            //     else
            //     {
            //         //  Pathfind to the target
            //         Goto(body.gridPosition.x + Random.Range(-1, 1), body.gridPosition.y + Random.Range(-1, 1));
            //     }
            // }
            // else
            // {
            //     FindResource();

            //     //  If we can't find a resource, wander around
            //     if ( !HasValidTarget() )
            //         Goto(
            //             Random.Range(gridPosition.x - 4, gridPosition.x + 4),
            //             Random.Range(gridPosition.x - 4, gridPosition.x + 4)
            //             );
            // }
        //}
    }
    

    private void FindBuilding(List<TerrainBuilding> blacklist = null)
    {
        TerrainBuilding nearestBuilding = null;

        //  Find the nearest tree within range
        int shortestDistance = cellSearchDistance * cellSearchDistance; //  Square the distance
        foreach (TerrainBuilding tree in ResourceManager.GetLumberMills())
        {
            if (tree == null) continue;   //  TODO trim null values from resource manager
            if (blacklist != null && blacklist.Contains(tree)) continue;

            Body body = tree.GetComponent<Body>();
            int distance = (int)Util.DistanceUnsquared(gridPosition, body.gridPosition);

            if (distance <= shortestDistance)
            {
                shortestDistance = distance;
                nearestBuilding = tree;
            }
        }

        if (nearestBuilding != null)
        {
            targetBuilding = nearestBuilding;
        }
    }

    private void FindResource(List<ResourceNode> blacklist = null)
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
}
