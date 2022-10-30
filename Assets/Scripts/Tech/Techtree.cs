using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TechNode
{
    public Tech tech;
    public List<Tech> requirements;
    public int researchCost;
    public int researchInvested;
    public Vector2 UIposition;

    public TechNode(Tech tech, List<Tech> reqs, int cost, Vector2 position)
    {
        this.tech = tech;
        this.requirements = reqs;
        this.researchCost = cost;
        this.researchInvested = 0;
        this.UIposition = position;
    }
}

[CreateAssetMenu(menuName = "Core/new Techtree")]
public class Techtree : ScriptableObject
{
    public List<TechNode> tree;

    public bool AddNode(Tech tech, Vector2 UIpos)
    {
        int tIdx = FindTechIndex(tech);
        if (tIdx == -1)
        {
            tree.Add(new TechNode(tech, new List<Tech>(), 0, UIpos));
            return true;
        }
        else
            return false;
    }

    public void DeleteNode(Tech tech)
    {
        tree.RemoveAt(FindTechIndex(tech));
        foreach(TechNode tn in tree)
        {
            if (tn.requirements.Contains(tech)) tn.requirements.Remove(tech);
        }
    }

    public int FindTechIndex(Tech tech)
    {
        for (int i = 0; i < tree.Count; i++)
        {
            if (tree[i].tech == tech) 
                return i;
        }

        return -1;
    }

    public bool DoesLeadsToInCascade(int query, int subject)
    {
        foreach (Tech t in tree[query].requirements)
        {
            if (t == tree[subject].tech)
                return true;
            
            if (DoesLeadsToInCascade(FindTechIndex(t), subject))
                return true;
        }
        return false;
    }

    public bool IsConnectible(int incomingNodeIdx, int outgoingNodeIdx)
    {
        if (incomingNodeIdx == outgoingNodeIdx)
            return false;

        return !(DoesLeadsToInCascade(incomingNodeIdx, outgoingNodeIdx) || DoesLeadsToInCascade(outgoingNodeIdx, incomingNodeIdx));
    }

    public HashSet<Tech> GetAllPastRequirements(int nodeIdx, bool includeSelfRequirements = true)
    {
        HashSet<Tech> allRequirements = (includeSelfRequirements) ? new HashSet<Tech>(tree[nodeIdx].requirements) : new HashSet<Tech>();
        foreach (Tech t in tree[nodeIdx].requirements)
        {
            allRequirements.UnionWith(GetAllPastRequirements(FindTechIndex(t)));
        }
        return allRequirements;
    }

    public void CorrectRequirementCascades(int idx)
    {
        HashSet<Tech> allConnectedThroughChildren = GetAllPastRequirements(idx, false);
        foreach (Tech t in allConnectedThroughChildren)
        {
            if (tree[idx].requirements.Contains(t))
                tree[idx].requirements.Remove(t);
        }
    }
}
