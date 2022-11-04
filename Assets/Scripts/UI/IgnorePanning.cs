using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class IgnorePanning : MonoBehaviour
{
    private PlayerManager playerManager;
    private Hand firstHand;
    private Hand secondHand;

    void Start()
    {
        playerManager = PlayerManager.Instance;
    }
    void OnTriggerEnter(Collider other)
    {        
        Hand hand = other.GetComponentInParent<Hand>();
        if (hand)
        {   
            if (!firstHand && secondHand != hand)
                firstHand = hand;
            
            if (!secondHand && firstHand != hand)
                secondHand = hand;

            playerManager.DisableGripPanning(hand);   

        }
    }

    void OnTriggerExit(Collider other)
    {
        Hand hand = other.GetComponentInParent<Hand>();
        if (hand)
        {
            if (firstHand && firstHand == hand)
                firstHand = null;
            
            if (secondHand && secondHand == hand)
                secondHand = null;

            playerManager.EnableGripPanning(hand);   
        }
    }

    void OnDestroy()
    {
        if (firstHand)
            playerManager.EnableGripPanning(firstHand);
        
        if (secondHand)
            playerManager.EnableGripPanning(secondHand);
    }
}
