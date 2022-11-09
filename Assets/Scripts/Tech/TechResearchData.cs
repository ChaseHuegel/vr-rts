using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Audio;
using UnityEngine;
using UnityEngine.Events;
using System;

[Serializable]
public enum FactionType
{
    None,
    Self,
    Ally,
    Enemy,
    All,
}

[CreateAssetMenu(fileName = "New TechResearch Data", menuName = "RTS/Tech/Tech Research Data")]
public class TechResearchData : TechBase
{
    [Header("Research")]
    public float upgradeAmount = 1.0f;

    public FactionType targetFaction;

    public List<TechBase> targets;

    public UnityEvent OnExecute;

    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        PlayerManager.Instance.CompleteResearch(this);

        // Unlock tech node
        //PlayerManager.Instance.Faction.techTree.SetNodeToUnlockedAndResearched(this);

        //Debug.LogFormat("{0} research complete.", this.title);
    }
}

