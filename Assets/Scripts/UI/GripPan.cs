using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class GripPan : MonoBehaviour
{
    [Header("Grip Scaling")]

    [Tooltip("Transform to scale to change size of player.")]
    public Transform targetTransform;

    [Tooltip("Minimum world scale of transform.")]
    public float minScale = 0.20f; // 

    [Tooltip("Maximum world scale of transform.")]
    public float maxScale = 5.0f; // 

    [Tooltip("Sensitivity and inversion of movement required.")]
    public float scalingSensitivity = 10.0f; // 

    [Header("Grip Panning")]
    public SteamVR_Action_Boolean GripOnOff;
    public float floorHeight = 0f;
    public float panMovementRate = 3.0f;
    public bool useMomentum = true;
    public float momentumStrength = 3.0f;
    public float momentumDrag = 5.0f;
    bool isPanning;
    bool isGliding;
    bool isScaling;
    Vector3 initialGripPosition;
    Hand currentHand;
    Hand otherHand;
    private Vector3 movementVector;
    private Vector3 glidingVector;
    private float glideTimePassed;
    private Vector3 startGrabPosition;
    private Vector3 endGrabPosition;
    float magnitude;
    bool isRightHandPanEnabled = true;
    bool isLeftHandPanEnabled = true;
    public bool IsScalingEnabled() => isRightHandPanEnabled && isLeftHandPanEnabled;
    private bool isRightGripPressed => GripOnOff.GetState(SteamVR_Input_Sources.RightHand);
    private bool isLeftGripPressed => GripOnOff.GetState(SteamVR_Input_Sources.LeftHand);
    private bool isScalingStarting() => isRightGripPressed && isLeftGripPressed && !isScaling;
     private float startScale = 1.0f;
    private float initialHandDistance;

    // Start is called before the first frame update
    void Start()
    {
        startScale = targetTransform.localScale.x;
                
        GripOnOff.AddOnStateDownListener(OnRightGripPressed, SteamVR_Input_Sources.RightHand);
        GripOnOff.AddOnStateUpListener(OnRightGripReleased, SteamVR_Input_Sources.RightHand);
        GripOnOff.AddOnStateDownListener(OnLeftGripPressed, SteamVR_Input_Sources.LeftHand);
        GripOnOff.AddOnStateUpListener(OnLeftGripReleased, SteamVR_Input_Sources.LeftHand);
    }

    public void DisablePanning(Hand hand)
    {
        if (hand.handType == SteamVR_Input_Sources.RightHand)
        {
            isRightHandPanEnabled = false;
            if (currentHand == hand)
                currentHand = null;
        }

        else if (hand.handType == SteamVR_Input_Sources.LeftHand)
        {
            isLeftHandPanEnabled = false;
            if (currentHand == hand)
                currentHand = null;
        }
    }

    public void EnablePanning(Hand hand)
    {
        if (hand.handType == SteamVR_Input_Sources.RightHand)
            isRightHandPanEnabled = true;

        else if (hand.handType == SteamVR_Input_Sources.LeftHand)
            isLeftHandPanEnabled = true;
    }

    
    // Update is called once per frame
    void Update()
    {
        if (isScalingStarting() && IsScalingEnabled())
        {
            initialHandDistance = Mathf.Abs(currentHand.transform.localPosition.x - otherHand.transform.localPosition.x);
            startScale = targetTransform.localScale.x;
            isScaling = true;
            isPanning = false;
        }

        if (isScaling && IsScalingEnabled())
        {
            if (!isRightGripPressed && !isLeftGripPressed)
            {
                isScaling = false;
                return;
            }

            isGliding = false;
            float currentHandDistance = Mathf.Abs(currentHand.transform.localPosition.x - otherHand.transform.localPosition.x);

            float distanceDelta = (currentHandDistance - initialHandDistance);
            distanceDelta *= -scalingSensitivity; 
            float newScale = startScale + (distanceDelta);            
            float clampedNewScale = Mathf.Clamp(newScale, minScale, maxScale);
            //clampedNewScale = Remap(clampedNewScale, minScale, maxScale, maxScale, minScale);
            targetTransform.localScale = new Vector3(clampedNewScale, clampedNewScale, clampedNewScale);
            //panStartPosition = panHandTransform.position;

        }
        else if (isPanning && currentHand)
        {
            movementVector = initialGripPosition - currentHand.panTransform.position;;
            Vector3 adjustedMovementVector = movementVector * panMovementRate;
            Player.instance.transform.position += adjustedMovementVector;
            initialGripPosition = currentHand.panTransform.position;
            glideTimePassed += Time.deltaTime;
        }
        else if (isGliding)
        {
            magnitude -= momentumDrag * Time.deltaTime / Player.instance.transform.localScale.x;
            if (magnitude < 0) magnitude = 0;

            Player.instance.transform.position += glidingVector * magnitude * Time.deltaTime;
        }

        //  Don't let player go below the 'floor'
        // RaycastHit hit;
        // Vector3 sourceLocation = Player.instance.hmdTransform.position;
        // sourceLocation.y += 10.0f;
        // Physics.Raycast(sourceLocation, Vector3.down, out hit, 30.0f, LayerMask.GetMask("Terrain"));

        // float min = hit.point.y + floorHeight;
        // if (Player.instance.hmdTransform.position.y < min)
        // {
        //     float requiredDistance = min - Player.instance.hmdTransform.position.y;
        //     float newFeetPosition = Player.instance.transform.position.y + requiredDistance;
        //     Player.instance.transform.position = new Vector3(Player.instance.transform.position.x, newFeetPosition, Player.instance.transform.position.z);
        // }

        // if (Player.instance.hmdTransform.position.y < floorHeight)
        // {
        //     // float requiredDistance = floorHeight - Player.instance.hmdTransform.position.y;
        //     // float newFeetPosition = Player.instance.transform.position.y + requiredDistance;
        //     // Player.instance.transform.position = new Vector3(Player.instance.transform.position.x, newFeetPosition, Player.instance.transform.position.z);
        // }
    }

    public void OnRightGripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {    
        // If scaling is active, releasing any grip cancels scaling
        if (isScaling)
        {
            isScaling = false;
            return;
        }

        if (!isRightHandPanEnabled) return;

        if (isPanning && currentHand != null && currentHand.handType == fromSource)
        {
            isPanning = false;

            endGrabPosition = currentHand.panTransform.position;

            if (useMomentum && glideTimePassed > 0.0001f)
            {
                isGliding = true;
                glidingVector = endGrabPosition - startGrabPosition;
                magnitude = (glidingVector.magnitude / Mathf.Max(glideTimePassed, 0.0001f)) * momentumStrength;
                glidingVector.Normalize();
            }

            currentHand = null;
        }
    }

    public void OnRightGripPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (isRightHandPanEnabled && !isScaling)
        {
            if (Player.instance.rightHand.hoveringInteractable == null)
            {
                currentHand = Player.instance.rightHand;
                otherHand = Player.instance.leftHand;
                initialGripPosition = currentHand.panTransform.position;
                isPanning = true;
            }
            isGliding = false;
            startGrabPosition = currentHand.panTransform.position;
            glideTimePassed = 0.0f;
        } 
    }

    public void OnLeftGripReleased(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        // If scaling is active, releasing any grip cancels scaling
        if (isScaling)
        {
            isScaling = false;
            return;
        }

        if (!isLeftHandPanEnabled) return;

        if (isPanning && currentHand != null && currentHand.handType == fromSource)
        {
            isPanning = false;

            endGrabPosition = currentHand.panTransform.position;

            if (useMomentum && glideTimePassed > 0.0001f)
            {
                isGliding = true;

                glidingVector = endGrabPosition - startGrabPosition;
                magnitude = (glidingVector.magnitude / Mathf.Max(glideTimePassed, 0.0001f)) * momentumStrength;
                glidingVector.Normalize();
            }

            currentHand = null;
        }
    }

    public void OnLeftGripPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (isLeftHandPanEnabled && !isScaling)
        {
            if (Player.instance.leftHand.hoveringInteractable == null)
            {
                currentHand = Player.instance.leftHand;
                otherHand = Player.instance.rightHand;
                initialGripPosition = currentHand.panTransform.position;
                isPanning = true;
            }

            isGliding = false;
            startGrabPosition = currentHand.panTransform.position;
            glideTimePassed = 0.0f;
        }
    }

    public static float Remap(float from, float fromMin, float fromMax, float toMin, float toMax)
    {
        var fromAbs = from - fromMin;
        var fromMaxAbs = fromMax - fromMin;

        var normal = fromAbs / fromMaxAbs;

        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;

        var to = toAbs + toMin;

        return to;
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