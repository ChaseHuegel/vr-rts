using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PalmMenu : MonoBehaviour
{
    // public Hand leftHand;
    // public Hand rightHand;

    InteractionPointer objectPlacementPointer;
    Hand menuHand;
    Hand selectionHand;
    
    // Start is called before the first frame update
    void Start()
    {
        menuHand = Player.instance.leftHand;
        selectionHand = Player.instance.rightHand;      
        objectPlacementPointer = FindObjectOfType<InteractionPointer>();          
    }

    public void Show(Transform parent)
    {
        gameObject.transform.SetParent(parent);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
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
}
