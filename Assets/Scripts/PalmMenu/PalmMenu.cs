using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PalmMenu : MonoBehaviour
{
    // public Hand leftHand;
    // public Hand rightHand;


    Hand menuHand;
    Hand selectionHand;
    
    // Start is called before the first frame update
    void Start()
    {
        menuHand = Player.instance.leftHand;
        selectionHand = Player.instance.rightHand;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Spawn(GameObject objectToSpawn)
    {
        GameObject go = Instantiate(objectToSpawn) as GameObject;
        selectionHand.AttachObject(go, GrabTypes.Scripted);
    }
}
