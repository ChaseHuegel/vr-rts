using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class LookAtAndReset : MonoBehaviour
{
    private bool autohide = true;
    private float autohideDelay = 10.0f;
    private float autohideRadius = 1.5f;
    public float rotationSpeed = 1.0f;
    public Transform target;   
    private float radiusExitTime; 
    private bool timerStarted;
    private BuildingHoverDisplay buildingHoverDisplay;

    void Awake()
    {
        if (!target) target = Player.instance.trackingOriginTransform;
        buildingHoverDisplay = GetComponentInParent<BuildingHoverDisplay>();
    }

    void Start()
    {
        radiusExitTime = Time.time;
    }
    
    // Update is called once per frame
    void Update()
    {   
        if (target)
        {
            Vector3 t = target.position - transform.position;
            t.y = 0;
            Quaternion rot = Quaternion.LookRotation(t);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotationSpeed);
        }

        float distance = (target.transform.position - transform.position).magnitude;
        
        if (timerStarted)
        {
            if (Time.time - radiusExitTime >= autohideDelay)
            {
                buildingHoverDisplay.Hide();
                timerStarted = false;
            }
        }
        else
            if (distance >= autohideRadius)
            {
                timerStarted = true;
                radiusExitTime = Time.time;
            }
        

        Debug.Log("distance: " + distance + "Time: " + (Time.time - radiusExitTime).ToString());
    }

    public void SetAutohideParameters(bool autohide, float delay, float radius)
    {
        this.autohide = autohide;
        autohideDelay = delay;
        autohideRadius = radius;
    }
}
