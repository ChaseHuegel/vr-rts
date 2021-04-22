using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class GripPan : MonoBehaviour
{
    // Reference to the hand for grip
    public SteamVR_Input_Sources handRight;
    public SteamVR_Input_Sources handLeft;
    public SteamVR_Action_Boolean GripOnOff;
    public GameObject player;
    public Valve.VR.InteractionSystem.Hand RightHand;
    public Valve.VR.InteractionSystem.Hand LeftHand;
    public float panRate = 3.0f;
    bool isPanning;
    Vector3 panStart;
    Transform panHand;
    SteamVR_Input_Sources currentHand;

    // Start is called before the first frame update
    void Start()
    {
        GripOnOff.AddOnStateDownListener(GripOn, handRight);
        GripOnOff.AddOnStateUpListener(GripOff, handRight);
        GripOnOff.AddOnStateDownListener(GripOn, handLeft);
        GripOnOff.AddOnStateUpListener(GripOff, handLeft);
    }

    // Update is called once per frame
    void Update()
    {
        if (isPanning)
        {
            Vector3 dist = panStart - panHand.transform.position;
            Vector3 newDist = dist * panRate;
            player.transform.position += newDist;
            panStart = panHand.transform.position;
        }
    }

    public void GripOff(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (isPanning && currentHand == fromSource)
            isPanning = false;
    }

    public void GripOn(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource == SteamVR_Input_Sources.RightHand)
        {
            if (RightHand.hoveringInteractable == null)
            {    
                panHand = RightHand.transform; 
                panStart = panHand.transform.position; 
                isPanning = true;
                currentHand = fromSource; 
            }
        }
        else if (fromSource == SteamVR_Input_Sources.LeftHand)
        {
            if (LeftHand.hoveringInteractable == null)
            {    
                panHand = LeftHand.transform;
                panStart = panHand.transform.position; 
                isPanning = true;
                currentHand = fromSource; 
            }
        }        
             

                             
    }
}

//         if (RightHand.hoveringInteractable == null && LeftHand.hoveringInteractable == null)
//         {
//             if (fromSource == SteamVR_Input_Sources.RightHand)
//                 panHand = RightHand.transform; 
//             else if (fromSource == SteamVR_Input_Sources.LeftHand)
//                 panHand = LeftHand.transform;

//             panStart = panHand.transform.position; 
//             isPanning = true;

//             if (fromSource != currentHand)
//                 currentHand = fromSource;
//         }      