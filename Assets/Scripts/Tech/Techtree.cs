using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TechNode
{
    public TechBase tech;

    public bool unlocked;
    public bool researched;
    public bool enabled;
    public List<TechBase> techRequirements;    
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

    public delegate void NodeUnlocked(TechNode node);
    public static event NodeUnlocked OnNodeUnlocked;

    public delegate void NodeLocked(TechNode node);
    public static event NodeLocked OnNodeLocked;

    public delegate void NodeResearched(TechNode node);
    public static event NodeResearched OnNodeResearched;

    public delegate void NodeEnabled(TechNode node);
    public static event NodeEnabled OnNodeEnabled;

    public delegate void NodeDisabled(TechNode node);
    public static event NodeDisabled OnNodeDisabled;

    void Start()
    {
        RefreshNodes();
    }

    public void RefreshNodes()
    {
        foreach (TechNode techNode in tree)
        {
            bool requirementsPassed = true;

            if (techNode.techRequirements.Count <= 0)
                continue;
                
            foreach (TechBase req in techNode.techRequirements)
            {
                requirementsPassed = FindNode(req).researched;

                if (requirementsPassed)
                    continue;
                else
                    break;
            }
            
            if (requirementsPassed)
            {
                techNode.unlocked = true;
                if (OnNodeUnlocked != null)
                    OnNodeUnlocked(techNode);
            }
            else
            {
                techNode.unlocked = false;
                if (OnNodeLocked != null)
                    OnNodeLocked(techNode);
            }

            if (techNode.unlocked && techNode.researched &&
                PlayerManager.Instance.CanAffordTech(techNode.tech))
            {
                techNode.enabled = true;
                if (OnNodeEnabled != null)
                    OnNodeEnabled(techNode);
            }
            else
            {
                techNode.enabled = false;
                if (OnNodeDisabled != null)
                    OnNodeDisabled(techNode);
            }
        }           
    }

#if UNITY_EDITOR
    public void RefreshNodesForEditor()
    {
        foreach (TechNode techNode in tree)
        {
            bool requirementsPassed = true;

            if (techNode.techRequirements.Count <= 0)
                continue;

            foreach (TechBase req in techNode.techRequirements)
            {
                requirementsPassed = FindNode(req).researched;

                if (requirementsPassed)
                    continue;
                else
                    break;
            }

            if (requirementsPassed)
                techNode.unlocked = true;
                if (OnNodeUnlocked != null)
                    OnNodeUnlocked(techNode);
            else
                techNode.unlocked = false;

            if (techNode.unlocked && techNode.researched &&
                PlayerManager.Instance.CanAffordTech(techNode.tech))
                techNode.enabled = true;
            else
                techNode.enabled = false;
        }
    }
#endif

    public bool AddNode(TechBase tech, Vector2 UIpos)
    {
        int tIdx = FindTechIndex(tech);
        if (tIdx == -1)
        {
            tree.Add(new TechNode(tech, new List<TechBase>(), UIpos));
            RefreshNodes();
            return true;
        }
        else
            return false;
    }

    public void UnlockTech(TechBase tech)
    {
        TechNode node = FindNode(tech);
        node.unlocked = true;
        RefreshNodes();

        if (OnNodeUnlocked != null)
            OnNodeUnlocked(node);
    }

    public void ResearchTech(TechBase tech)
    {
        TechNode node = FindNode(tech);
        node.researched = true;
        RefreshNodes();

        if (OnNodeResearched != null)
            OnNodeResearched(node);
    }

    public bool IsUnlocked(TechBase tech)
    {
        TechNode node = FindNode(tech);
        return node != null ? node.unlocked : false;
    }

    public bool IsResearched(TechBase tech)
    {
        TechNode node = FindNode(tech);
        return node != null ? node.researched : false;
    }

    public TechNode FindNode(TechBase tech)
    {
        return tree.Find(x => x.tech == tech);
    }

    public void DeleteNode(TechBase tech)
    {
        tree.RemoveAt(FindTechIndex(tech));
        foreach(TechNode tn in tree)
        {
            if (tn.techRequirements.Contains(tech)) tn.techRequirements.Remove(tech);
        }

        RefreshNodes();
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
