using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swordfish.Audio
{

[CreateAssetMenu(fileName = "New Item", menuName = "Swordfish/Audio/Sound Element")]
public class SoundElement : ScriptableObject
{
    [SerializeField] private AudioClip[] variants;

    public AudioClip GetClip()
    {
        return variants[
            variants.Length > 1 ? Random.Range(0, variants.Length) : 0
        ];
    }
}

}