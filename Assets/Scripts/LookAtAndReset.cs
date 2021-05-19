using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class LookAtAndReset : MonoBehaviour
{
    public float rotationSpeed = 1.0f;
    public Transform target;   
    void Awake()
    {
        if (!target) target = Player.instance.hmdTransform;
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
