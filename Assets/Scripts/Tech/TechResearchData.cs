using System.Collections;
using System.Collections.Generic;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "New TechResearch Data", menuName = "RTS/Tech/Tech Data")]
public class TechResearchData : TechBase
{
    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        PlayerManager.Instance.CompleteResearch(this);

        // Unlock tech node
        //PlayerManager.Instance.Faction.techTree.SetNodeToUnlockedAndResearched(this);

        //Debug.LogFormat("{0} research complete.", this.title);
    }
}