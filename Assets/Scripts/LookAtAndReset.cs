using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class LookAtAndReset : MonoBehaviour
{
    float rotationSpeed = 1.0f;
    public Transform target;    
    void Start()
    {
        if (!target) target = Player.instance.transform;
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
    }
}
