using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class SpawnObject : MonoBehaviour
{   
    public GameObject prefabCube;
    public GameObject prefabSphere;

public 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnCube()
    {
        GameObject a = Instantiate(prefabCube) as GameObject;
        // Debug.Log(obj);
        // Hand hand = obj as Hand;
        // hand.AttachObject(a, GrabTypes.Pinch);
    }

    public void SpawnSphere()
    {
        GameObject a = Instantiate(prefabSphere) as GameObject;
    }
}
