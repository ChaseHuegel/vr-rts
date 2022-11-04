using System.Collections;
using System.Collections.Generic;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tech Unlock", menuName = "RTS/Tech/Tech Unlocker")]
public class TechUnlocker : TechBase
{
    bool unlockTechNode;

    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        // Unlock tech node
        PlayerManager.Instance.faction.techTree.SetNodeToUnlocked(this);

        Debug.LogFormat("{0} research complete.", this.title);
    }
}