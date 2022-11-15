using System.Collections;
using System.Collections.Generic;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "Epoch Upgrade", menuName = "RTS/Tech/Epoch Upgrade")]
public class EpochUpgrade : TechBase
{
    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        PlayerManager.Instance.faction.techTree.UnlockTech(this);
        PlayerManager.Instance.faction.techTree.ResearchTech(this);
    }
}