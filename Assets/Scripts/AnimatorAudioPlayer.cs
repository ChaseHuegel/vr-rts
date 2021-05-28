using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorAudioPlayer : MonoBehaviour
{   
    private AudioSource audioSource;

    private Unit unit;


    // TODO: Change to events and have parent subscribe to them?
    void Start()
    {
        unit = GetComponentInParent<Unit>();

        if (!(audioSource = GetComponentInParent<AudioSource>()))
        {
            Debug.Log("No Audiosource component found in parent.", this);
            audioSource = new AudioSource();
        }        
    } 

    public void AnimatorPlayAudio(string clipName)
    {
        if (audioSource)
            audioSource.PlayOneShot(GameMaster.GetAudio(clipName).GetClip());
    }

    public void LaunchProjectile()
    {
        if (unit)
            unit.LaunchProjectile();
    }
}
