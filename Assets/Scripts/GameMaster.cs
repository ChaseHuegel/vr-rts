using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;

public class GameMaster : Singleton<GameMaster>
{
    public AudioDatabase audioDatabase;

    public static AudioDatabase GetAudioDatabase() { return Instance.audioDatabase; }
    public static SoundElement GetAudio(string name) { return Instance.audioDatabase.Get(name); }
}
