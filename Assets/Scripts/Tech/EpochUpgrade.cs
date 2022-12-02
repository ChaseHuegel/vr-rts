using System.Collections;
using System.Collections.Generic;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "Epoch Upgrade", menuName = "RTS/Tech/Epoch Upgrade")]
public class EpochUpgrade : TechBase
{
    public int epochId;
    public int requiredBuildingCount;
    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);        
    }
}
