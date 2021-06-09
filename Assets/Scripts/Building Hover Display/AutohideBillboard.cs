using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class AutohideBillboard : MonoBehaviour
{
    public delegate void OnAutohide();
    public event OnAutohide onAutoHide;

    [Header("Options")]
    public Transform faceTarget;  
    public bool startHidden = true;
    
    [Tooltip("Autohide events are fired at the requisite time to notify listeners for external control of hiding.")]
    public bool autohide = true;

    [Tooltip("Disable AutohideBillboard component when onAutoHide event is called.")]
    public bool disableSelf = true;

    [Tooltip("Delay to hide the menu after the target has crossed the autohideDistance threshold.")]
    public float autohideDelay = 15.0f;
    
    [Tooltip("Distance required from object to target before the autohide timer starts.")]
    public float autohideDistance = 2.0f;

    [Tooltip("The speed used when rotating to face the target.")]
    
    public float rotationSpeed = 5.0f;
    private float radiusExitTime; 
    private bool autohideTimerStarted;
    
    void Awake()
    {
        if (!faceTarget) faceTarget = Player.instance.hmdTransform;
    }

    void Start()
    {
        radiusExitTime = Time.time;
        Quaternion rot = faceTarget.transform.rotation;
        rot.z = rot.x = 0;
        transform.rotation = rot;
    }

    // Update is called once per frame
    void Update()
    {
        if (faceTarget)
        {
            float distance = Vector3.Distance(faceTarget.position, transform.position);
            if (distance > 0.75f)
            {
                //Vector3 t = (faceTarget.position - transform.position);
                //t.y = 0;
                //Quaternion rot = Quaternion.LookRotation(t);
                Quaternion rot = faceTarget.transform.rotation;// Quaternion.LookRotation(t);
                rot.z = rot.x = 0;
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotationSpeed);
            }

        }

        if (autohideTimerStarted)
        {
            if (Time.time - radiusExitTime >= autohideDelay)
            {
                  
                if (onAutoHide != null) 
                    onAutoHide();
                
                if (disableSelf)
                    this.enabled = false;

                autohideTimerStarted = false;
            }
        }
        else
        {
            float distance = (faceTarget.transform.position - transform.position).magnitude;
            if (distance >= autohideDistance)
            {
                autohideTimerStarted = true;
                radiusExitTime = Time.time;
            }
        }
    }
    public void SetAutohideParameters(bool autohide, bool disableSelf, float delay, float distance)
    {
        this.autohide = autohide;
        autohideDelay = delay;
        autohideDistance = distance;
        this.disableSelf = disableSelf;
    }

    
}
