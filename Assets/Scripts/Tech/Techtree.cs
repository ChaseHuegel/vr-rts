using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TechNode
{
    public TechBase tech;
    public bool unlocked;
    public List<TechBase> techRequirements;    
    public bool researched;    
    public Vector2 UIposition;

    public TechNode(TechBase tech, List<TechBase> reqs, Vector2 position, bool unlocked = false, bool researched = false )
    {
        this.tech = tech;
        this.techRequirements = reqs;
        this.researched = researched;
        this.unlocked = unlocked;
        this.UIposition = position;
    }

}

[CreateAssetMenu(menuName = "RTS/Tech/New Tech Tree")]
public class TechTree : ScriptableObject
{
    public List<TechNode> tree;

    public bool AddNode(TechBase tech, Vector2 UIpos)
    {
        int tIdx = FindTechIndex(tech);
        if (tIdx == -1)
        {
            tree.Add(new TechNode(tech, new List<TechBase>(), UIpos));
            return true;
        }
        else
            return false;
    }

    public void DeleteNode(TechBase tech)
    {
        tree.RemoveAt(FindTechIndex(tech));
        foreach(TechNode tn in tree)
        {
            if (tn.techRequirements.Contains(tech)) tn.techRequirements.Remove(tech);
        }
    }

    public int FindTechIndex(TechBase tech)
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
        foreach (TechBase t in tree[query].techRequirements)
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

    public HashSet<TechBase> GetAllPastRequirements(int nodeIdx, bool includeSelfRequirements = true)
    {
        HashSet<TechBase> allRequirements = (includeSelfRequirements) ? new HashSet<TechBase>(tree[nodeIdx].techRequirements) : new HashSet<TechBase>();
        foreach (TechBase t in tree[nodeIdx].techRequirements)
        {
            allRequirements.UnionWith(GetAllPastRequirements(FindTechIndex(t)));
        }
        return allRequirements;
    }

    public void CorrectRequirementCascades(int idx)
    {
        HashSet<TechBase> allConnectedThroughChildren = GetAllPastRequirements(idx, false);
        foreach (TechBase t in allConnectedThroughChildren)
        {
            if (tree[idx].techRequirements.Contains(t))
                tree[idx].techRequirements.Remove(t);
        }
    }
}
