using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class LookAtAndReset : MonoBehaviour
{
    public Transform target;
    private Vector3 oldPosition;
    private Quaternion oldRotation;
    // Start is called before the first frame update
    void Start()
    {
        oldPosition = transform.position;
        oldRotation = transform.rotation;
        target = Player.instance.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (target)
        {
            Vector3 t = target.position;
            t.y = transform.position.y;
            transform.LookAt(t);
        }
    }

    // transform.position = oldPosition;
    // transform.rotation = oldRotation;
}
