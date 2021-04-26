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
    
    public void Toggle()
    {
        if (menuHand == null)
            menuHand = Player.instance.leftHand;
        if (selectionHand == null)
            selectionHand = Player.instance.rightHand;

        if (gameObject.activeSelf == true)
        {
            gameObject.SetActive(false);  
            menuHand.useHoverSphere = true;
            menuHand.useFingerJointHover = true;
            //selectionHand.useHoverSphere = true;                      
        }    
        else
        {   
            menuHand = Player.instance.leftHand;
            menuHand.useHoverSphere = false;
            menuHand.useFingerJointHover = false;
            //selectionHand.useHoverSphere = false;
            gameObject.SetActive(true);          
        }
    }

    public void Spawn(GameObject objectToSpawn)
    {
        //GameObject go = Instantiate(objectToSpawn) as GameObject;
        
        //selectionHand.AttachObject(go, GrabTypes.Scripted);

        //go.gameObject.transform.position = new Vector3();
    }
}
