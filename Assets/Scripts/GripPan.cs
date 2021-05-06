using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class GripPan : MonoBehaviour
{
    // Reference to the hand for grip
    public SteamVR_Input_Sources handRight;
    public SteamVR_Input_Sources handLeft;
    public SteamVR_Action_Boolean GripOnOff;
    public Valve.VR.InteractionSystem.Hand RightHand;
    public Valve.VR.InteractionSystem.Hand LeftHand;

    public float floorHeight = 0f;

    public float panMovementRate = 3.0f;
    public bool useMomentum = true;
    public float momentumStrength = 1.0f;
    public float momentumDrag = 5.0f;

    bool isPanning;
    bool isGliding;
    Vector3 panStartPosition;
    Transform panHand;
    SteamVR_Input_Sources currentHand;

    private Vector3 movementVector;
    private Vector3 glidingVector;
    private float glideTimePassed;
    private Vector3 grabPosition;
    private Vector3 grabOffPosition;
    float magnitude;
    Vector3 velocity;
    float grabTime;

    bool isPanEnabled;
    bool isRightHandPanEnabled = true;
    bool isLeftHandPanEnabled = true;

    // Start is called before the first frame update
    void Start()
    {
        isPanEnabled = true;
        
        GripOnOff.AddOnStateDownListener(GripOn, handRight);
        GripOnOff.AddOnStateUpListener(GripOff, handRight);
        GripOnOff.AddOnStateDownListener(GripOn, handLeft);
        GripOnOff.AddOnStateUpListener(GripOff, handLeft);

    }

    public void DisablePanning(Hand hand)
    {
        if (hand == RightHand)
            isRightHandPanEnabled = false;
        else
            isLeftHandPanEnabled = false;
        
        // isRightHandPanEnabled = hand == RightHand;
        // isLeftHandPanEnabled = hand == LeftHand;
    }

    public void EnablePanning(Hand hand)
    {
        if (hand == RightHand)
            isRightHandPanEnabled = true;
        else
            isLeftHandPanEnabled = true;

        // isRightHandPanEnabled = hand != RightHand;
        // isLeftHandPanEnabled = hand != LeftHand;
    }

    // Update is called once per frame
    void Update()
    {           
        if (isPanning)
        {
            movementVector = panStartPosition - panHand.transform.position;
            Vector3 adjustedMovementVector = movementVector * panMovementRate;
            Player.instance.transform.position += adjustedMovementVector;
            panStartPosition = panHand.position;
            glideTimePassed += Time.deltaTime;
        }
        else if (isGliding)
        {
            magnitude -= momentumDrag * Time.deltaTime;
            if (magnitude < 0) magnitude = 0;

            Player.instance.transform.position += glidingVector * magnitude * Time.deltaTime;
        }

        //  Don't let player go below the 'floor'
        if (Player.instance.transform.position.y < floorHeight)
            Player.instance.transform.position = new Vector3(Player.instance.transform.position.x, floorHeight, Player.instance.transform.position.z);
    }

    public void GripOff(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (isPanning && currentHand == fromSource)
        {
            isPanning = false;

            grabOffPosition = panHand.position;

            if (useMomentum)
            {
                isGliding = true;

                glidingVector = grabOffPosition - grabPosition;
                magnitude = (glidingVector.magnitude / glideTimePassed) * momentumStrength;
                glidingVector.Normalize();
            }
        }

    }

    public void GripOn(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (fromSource == SteamVR_Input_Sources.RightHand && isRightHandPanEnabled)
        {
            if (RightHand.hoveringInteractable == null)
            {
                panHand = RightHand.transform;
                panStartPosition = panHand.transform.position;
                isPanning = true;
                currentHand = fromSource;
            }
            isGliding = false;
            grabPosition = panHand.position;
            glideTimePassed = 0.0f;
        }
        else if (fromSource == SteamVR_Input_Sources.LeftHand && isLeftHandPanEnabled)
        {
            if (LeftHand.hoveringInteractable == null)
            {
                panHand = LeftHand.transform;
                panStartPosition = panHand.transform.position;
                isPanning = true;
                currentHand = fromSource;
            }

            isGliding = false;
            grabPosition = panHand.position;
            glideTimePassed = 0.0f;
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