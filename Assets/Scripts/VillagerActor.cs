using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public class VillagerActor : Actor
{
    [SerializeField] protected int cellSearchDistance = 20;
    [SerializeField] protected ResourceNode targetNode;

    public bool HasValidTarget()
    {
        return (targetNode != null);
    }

    public override void Tick()
    {
        //  If we aren't pathfinding...
        if ( !HasValidPath() )
        {
            //  Pathfind to our target if we have one and havent reached it
            if ( HasValidTarget())
            {
                Body body = targetNode.GetComponent<Body>();

                if (Util.DistanceUnsquared(gridPosition, body.gridPosition) <= 1.5f)
                {
                    //  Reached our target
                    targetNode.decreaseCurrentResourceAmount(1000);
                    Debug.DrawRay(targetNode.transform.position, Vector3.up, Color.red, 0.5f);
                }
                else
                {
                    //  Pathfind to the target
                    Goto(body.gridPosition.x + Random.Range(-1, 1), body.gridPosition.y + Random.Range(-1, 1));
                }
            }
            else
            {
                FindResource();

                //  If we can't find a resource, wander around
                if ( !HasValidTarget() )
                    Goto(
                        Random.Range(gridPosition.x - 4, gridPosition.x + 4),
                        Random.Range(gridPosition.x - 4, gridPosition.x + 4)
                        );
            }
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
