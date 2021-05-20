using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorAudioPlayer : MonoBehaviour
{   
    private AudioSource audioSource;

    void Start()
    {
        if (!(audioSource = GetComponentInParent<AudioSource>()))
        {
            Debug.Log("No Audiosource component found in parent.", this);
            audioSource = new AudioSource();
        }        
    } 

    public void AnimatorPlayAudio(string clipName)
    {
        audioSource.PlayOneShot(GameMaster.GetAudio(clipName).GetClip());
    }
}
