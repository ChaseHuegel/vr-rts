using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public class ResourceManager : Singleton<ResourceManager>
{
    public GameObject treesFromChildrenTarget;

    public GameObject lumberMillsFromChildrenTarget;

    public List<ResourceNode> trees;

    public List<TerrainBuilding> lumberMills;

    public static List<ResourceNode> GetTrees() { return Instance.trees; }
    public static List<TerrainBuilding> GetLumberMills() { return Instance.lumberMills; }
    public void Start()
    {
        foreach (ResourceNode node in treesFromChildrenTarget.GetComponentsInChildren<ResourceNode>())
        {
            trees.Add(node);
        }

        foreach (TerrainBuilding building in lumberMillsFromChildrenTarget.GetComponentsInChildren<TerrainBuilding>())
        {
            lumberMills.Add(building);
        }
    }
}
