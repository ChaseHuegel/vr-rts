using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorAudioPlayer : MonoBehaviour
{    public void AnimatorPlayAudio(string clipName) 
    {
        AudioSource.PlayClipAtPoint(GameMaster.GetAudio(clipName).GetClip(), transform.position, 0.75f);
    }
}
