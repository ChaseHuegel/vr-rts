using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public class ResourceManager : Singleton<ResourceManager>
{
    public GameObject treesFromChildrenTarget;
    public GameObject goldFromChildrenTarget;
    public GameObject grainFromChildrenTarget;
    public GameObject lumberMillsFromChildrenTarget;
    public GameObject townHallsFromChildrenTarget;
    public GameObject granariesFromChildrenTarget;
    
    [Header("ResourceNodes")]
    public List<ResourceNode> trees;
    public List<ResourceNode> gold;
    public List<ResourceNode> grain;

    [Header("Buildings")]
    public List<TerrainBuilding> lumberMills;
    public List<TerrainBuilding> townHalls;
    public List<TerrainBuilding> granaries;

    public static List<ResourceNode> GetTrees() { return Instance.trees; }
    public static List<ResourceNode> GetGold() { return Instance.gold; }
    public static List<ResourceNode> GetGrain() { return Instance.grain; }

    public static List<TerrainBuilding> GetLumberMills() { return Instance.lumberMills; }
    public static List<TerrainBuilding> GetTownHalls() { return Instance.townHalls; }
    public static List<TerrainBuilding> GetGranaries() {return Instance.granaries; }

    public void Start()
    {
        foreach (ResourceNode node in treesFromChildrenTarget.GetComponentsInChildren<ResourceNode>())
        {
            trees.Add(node);
        }

        foreach (ResourceNode node in goldFromChildrenTarget.GetComponentsInChildren<ResourceNode>())
        {
            gold.Add(node);
        }

        foreach (ResourceNode node in grainFromChildrenTarget.GetComponentsInChildren<ResourceNode>())
        {
            grain.Add(node);
        }

        foreach (TerrainBuilding building in lumberMillsFromChildrenTarget.GetComponentsInChildren<TerrainBuilding>())
        {
            lumberMills.Add(building);
        }

        foreach (TerrainBuilding building in townHallsFromChildrenTarget.GetComponentsInChildren<TerrainBuilding>())
        {
            townHalls.Add(building);
        }

        foreach (TerrainBuilding building in granariesFromChildrenTarget.GetComponentsInChildren<TerrainBuilding>())
        {
            granaries.Add(building);
        }
    }
}
