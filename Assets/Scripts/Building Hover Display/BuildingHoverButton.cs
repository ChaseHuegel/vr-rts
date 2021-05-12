using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class BuildingHoverButton : MonoBehaviour
{
    public HandEvent onButtonDown;
    
    public void OnButtonDown(Hand hand)
    {
        onButtonDown.Invoke(hand);
    }
}
