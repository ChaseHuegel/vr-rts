using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorEventForwarder : MonoBehaviour
{
    private AudioSource audioSource;
    private UnitV2 unit;

    void Start()
    {
        unit = GetComponentInParent<UnitV2>();

        if (!(audioSource = GetComponentInParent<AudioSource>()))
        {
            Debug.Log("No Audiosource component found in parent.", this);
            audioSource = new AudioSource();
        }
    }

    /// <summary>
    /// Forwards a contact/strike event from the animator to the unit 
    /// this animator belongs to.
    /// </summary>
    /// <param name="audioClipName"></param>
    public void Strike(string audioClipName = "")
    {
    }

    // TODO: Remove this, don't think this is called anymore by any animators.
    public void PlayAudio(string clipName = "")
    {
        if (audioSource != null && clipName != "")
            audioSource.PlayOneShot(GameMaster.GetAudio(clipName).GetClip());
    }

    public void LaunchProjectile()
    {
        this.GetComponentInParent<UnitV2>().LaunchProjectile();
    }
}
