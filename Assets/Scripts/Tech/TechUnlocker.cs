using System.Collections;
using System.Collections.Generic;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tech Unlock", menuName = "RTS/Tech/Tech Unlocker")]
public class TechUnlocker : TechBase
{
    bool unlockTechNode;

    public List<TechBase> targetTechs;

    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);

        // Unlock tech nodes
        foreach (TechBase tech in targetTechs)
        {
            PlayerManager.Instance.faction.techTree.UnlockTech(tech);

            Debug.LogFormat("{0} unlocked.", tech.title);
        }
    }
}