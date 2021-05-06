using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class IgnorePanning : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {        
        Hand hand = other.GetComponentInParent<Hand>();
        if (hand)
        {
                PlayerManager.instance.DisableGripPanning(hand);   
        }
    }

    void OnTriggerExit(Collider other)
    {
        Hand hand = other.GetComponentInParent<Hand>();
        if (hand)
        {
            PlayerManager.instance.EnableGripPanning(hand);   
        }
    }
}
