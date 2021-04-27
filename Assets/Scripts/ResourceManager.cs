using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Navigation;

public class ResourceManager : Singleton<ResourceManager>
{
    public GameObject treesFromChildrenTarget;
    public List<ResourceNode> trees;

    public static List<ResourceNode> GetTrees() { return Instance.trees; }

    public void Start()
    {
        foreach (ResourceNode node in treesFromChildrenTarget.GetComponentsInChildren<ResourceNode>())
        {
            trees.Add(node);
        }
    }
}
