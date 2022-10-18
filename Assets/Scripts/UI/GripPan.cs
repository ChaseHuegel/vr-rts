using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class GripPan : MonoBehaviour
{
    // Transform to scale to change size of player
    public Transform scalingTransform;
    public SteamVR_Action_Boolean GripOnOff;

    public float floorHeight = 0f;

    public float panMovementRate = 3.0f;
    public bool useMomentum = true;
    public float momentumStrength = 1.0f;
    public float momentumDrag = 5.0f;

    bool isPanning;
    bool isGliding;
    bool isScaling;

    Vector3 panStartPosition;
    Transform panHandTransform;
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

    private Player player = null;
    private bool isRightGripPressed;
    private bool isLeftGripPressed;
    
    private float startScale = 1.0f;
    private float initialHandDistance;

    // Start is called before the first frame update
    void Start()
    {
        player = Valve.VR.InteractionSystem.Player.instance;
        startScale = scalingTransform.localScale.x;

        if (player == null)
        {
            Debug.LogError("<b>[SteamVR Interaction]</b> GripPan: No Player instance found in map.", this);
            Destroy(this.gameObject);
            return;
        }

        isPanEnabled = true;
        
        GripOnOff.AddOnStateDownListener(OnRightGripPressed, SteamVR_Input_Sources.RightHand);
        GripOnOff.AddOnStateUpListener(OnRightGripReleased, SteamVR_Input_Sources.RightHand);
        GripOnOff.AddOnStateDownListener(OnLeftGripPressed, SteamVR_Input_Sources.LeftHand);
        GripOnOff.AddOnStateUpListener(OnLeftGripReleased, SteamVR_Input_Sources.LeftHand);

    }

    public void DisablePanning(Hand hand)
    {
        if (hand == player.rightHand)
            isRightHandPanEnabled = false;
        else
            isLeftHandPanEnabled = false;
        
        // isRightHandPanEnabled = hand == RightHand;
        // isLeftHandPanEnabled = hand == LeftHand;
    }

    public void EnablePanning(Hand hand)
    {
        if (hand == player.rightHand)
            isRightHandPanEnabled = true;
        else
            isLeftHandPanEnabled = true;

        // isRightHandPanEnabled = hand != RightHand;
        // isLeftHandPanEnabled = hand != LeftHand;
    }  

    // Update is called once per frame
    void Update()
    {
        if (isRightGripPressed && isLeftGripPressed)
        {
            if (!isScaling)
            {
                initialHandDistance = Vector3.Distance(player.rightHand.transform.position, player.leftHand.transform.position);
                startScale = scalingTransform.localScale.x;
                isScaling = true;
                isPanning = false;                                           
            }
        }

        if (isScaling)
        {
            if (!isRightGripPressed && !isLeftGripPressed)
            {
                isScaling = false;
                return;
            }

            isGliding = false;
            
            float minScale = 0.20f; // Minimum world scale of transform
            float maxScale = 5.0f; // Maximum world scale of transform

            float currentHandDistance = Vector3.Distance(player.leftHand.transform.position, player.rightHand.transform.position);            
            float distanceDelta = (currentHandDistance - initialHandDistance);
            float newScale = startScale + (distanceDelta * -1.0f); // invert hand movement direction in relation to scaling
            float clampedNewScale = Mathf.Clamp(newScale, minScale, maxScale);                       
            scalingTransform.localScale = new Vector3(clampedNewScale, clampedNewScale, clampedNewScale);

            //Debug.LogFormat("initHandDist= {0} : curHandDist= {1} : distDelta= {2} : startScale= {3} : clampNewScale= {4}", initialHandDistance, currentHandDistance, distanceDelta, startScale, clampedNewScale);

            //panStartPosition = panHandTransform.position;

        }
        else if (isPanning)
        {
            movementVector = panStartPosition - panHandTransform.position;
            Vector3 adjustedMovementVector = movementVector * panMovementRate;
            Player.instance.transform.position += adjustedMovementVector;
            panStartPosition = panHandTransform.position;
            glideTimePassed += Time.deltaTime;
        }
        else if (isGliding)
        {
            magnitude -= momentumDrag * Time.deltaTime;
            if (magnitude < 0) magnitude = 0;

            Player.instance.transform.position += glidingVector * magnitude * Time.deltaTime;
            //transform.position = Vector3.Lerp(transform.position, transform.position + glidingVector * magnitude, Time.deltaTime);
        }


        //  Don't let player go below the 'floor'
        // if (Player.instance.transform.position.y < floorHeight)
        //     Player.instance.transform.position = new Vector3(Player.instance.transform.position.x, floorHeight, Player.instance.transform.position.z);
    }


    public void OnRightGripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {    
        isRightGripPressed = false;
        if (isScaling) return;

        if (isPanning && currentHand == fromSource)
        {
            isPanning = false;
            grabOffPosition = panHandTransform.position;

            if (useMomentum)
            {
                isGliding = true;

                glidingVector = grabOffPosition - grabPosition;
                magnitude = (glidingVector.magnitude / glideTimePassed) * momentumStrength;
                glidingVector.Normalize();
            }
        }
    }

    public void OnRightGripPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        isRightGripPressed = true;

        if (isRightHandPanEnabled && !isLeftGripPressed)
        {
            if (player.rightHand.hoveringInteractable == null)
            {
                panHandTransform = player.rightHand.transform;
                panStartPosition = panHandTransform.transform.position;
                isPanning = true;
                currentHand = fromSource;
            }
            isGliding = false;
            grabPosition = panHandTransform.position;
            glideTimePassed = 0.0f;
        } 
    }

    public void OnLeftGripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        isLeftGripPressed = false;
        if (isScaling) return;

        if (isPanning && currentHand == fromSource)
        {
            isPanning = false;
            grabOffPosition = panHandTransform.position;

            if (useMomentum)
            {
                isGliding = true;

                glidingVector = grabOffPosition - grabPosition;
                magnitude = (glidingVector.magnitude / glideTimePassed) * momentumStrength;
                glidingVector.Normalize();
            }
        }
    }

    public void OnLeftGripPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        isLeftGripPressed = true;

        if (isLeftHandPanEnabled && !isRightGripPressed)
        {
            if (player.leftHand.hoveringInteractable == null)
            {
                panHandTransform = player.leftHand.transform;
                panStartPosition = panHandTransform.transform.position;
                isPanning = true;
                currentHand = fromSource;
            }

            isGliding = false;
            grabPosition = panHandTransform.position;
            glideTimePassed = 0.0f;
        }
    }    
}

    // if (RightHand.hoveringInteractable == null && LeftHand.hoveringInteractable == null)
    // {
    //     if (fromSource == SteamVR_Input_Sources.RightHand)
    //         panHand = RightHand.transform;
    //     else if (fromSource == SteamVR_Input_Sources.LeftHand)
    //         panHand = LeftHand.transform;

    //     panStart = panHand.transform.position;
    //     isPanning = true;

    //     if (fromSource != currentHand)
    //         currentHand = fromSource;
    // }