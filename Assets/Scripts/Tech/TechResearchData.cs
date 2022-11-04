using System.Collections;
using System.Collections.Generic;
using Swordfish.Audio;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tech Data", menuName = "RTS/Tech/Tech Data")]
public class TechResearchData : TechBase
{
    public override void Execute(SpawnQueue spawnQueue)
    {
        base.Execute(spawnQueue);
        Debug.LogFormat("{0} research complete.", this.title);
    }
}