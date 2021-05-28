using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class IgnorePanning : MonoBehaviour
{
    PlayerManager playerManager;

    void Start()
    {
        playerManager = PlayerManager.instance;
    }
    void OnTriggerEnter(Collider other)
    {        
        Hand hand = other.GetComponentInParent<Hand>();
        if (hand)
        {
                playerManager.DisableGripPanning(hand);   
        }
    }

    void OnTriggerExit(Collider other)
    {
        Hand hand = other.GetComponentInParent<Hand>();
        if (hand)
        {
            playerManager.EnableGripPanning(hand);   
        }
    }
}
