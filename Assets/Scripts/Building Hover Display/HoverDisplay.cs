using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Valve.VR.InteractionSystem;
using UnityEngine.Events;

public class HoverDisplay : MonoBehaviour
{
    UnityEvent OnHoverDisplayShow;

    [Header("Options")]
    public bool startHidden = true;
    public bool autohide = true;
    [Tooltip("Delay to hide the menu after the target has crossed the autohideDistance threshold.")]
    float autohideDelay = 15.0f;
    [Tooltip("Distance required from object to target before the autohide timer starts.")]
    float autohideDistance = 2.0f;

    void Start()
    {
        
    }

    protected void Show()
    {
        OnHoverDisplayShow.Invoke();
    }
}
