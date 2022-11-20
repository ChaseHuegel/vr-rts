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
    public SteamVR_Action_Single TriggerOnOff;
    public float floorHeight = 0f;
    public float panMovementRate = 3.0f;
    public bool useMomentum = true;
    public float momentumStrength = 3.0f;
    public float momentumDrag = 5.0f;

    bool isPanning;
    bool isGliding;
    bool isScaling;

    bool isRightHandKnocking;
    bool isLeftHandKnocking;

    Vector3 initialGripPosition;
    Transform currentGripTransform;
    SteamVR_Input_Sources currentHand;

    private Vector3 movementVector;
    private Vector3 glidingVector;
    private float glideTimePassed;
    private Vector3 grabPosition;
    private Vector3 grabOffPosition;
    float magnitude;
    Vector3 velocity;
    float grabTime;

    //bool isPanEnabled;
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
        startScale = targetTransform.localScale.x;

        if (player == null)
        {
            Debug.LogError("<b>[SteamVR Interaction]</b> GripPan: No Player instance found in map.", this);
            Destroy(this.gameObject);
            return;
        }

        //isPanEnabled = true;
                
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
                Vector3 right = player.rightHand.transform.localPosition;                
                Vector3 left = player.leftHand.transform.localPosition;
                right.z = right.y = left.z = left.y = 0;
                initialHandDistance = Vector3.Distance(right, left);
                startScale = targetTransform.localScale.x;
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

            Vector3 right = player.rightHand.transform.localPosition;
            Vector3 left = player.leftHand.transform.localPosition;
            right.z = right.y = left.z = left.y = 0;

            float currentHandDistance = Vector3.Distance(right, left);
            float distanceDelta = (currentHandDistance - initialHandDistance);
            distanceDelta *= -scalingSensitivity; 
            float newScale = startScale + (distanceDelta);            
            float clampedNewScale = Mathf.Clamp(newScale, minScale, maxScale);
            //clampedNewScale = Remap(clampedNewScale, minScale, maxScale, maxScale, minScale);
            targetTransform.localScale = new Vector3(clampedNewScale, clampedNewScale, clampedNewScale);
            //panStartPosition = panHandTransform.position;

        }
        else if (isPanning)
        {
            movementVector = initialGripPosition - currentGripTransform.position;
            Vector3 adjustedMovementVector = movementVector * panMovementRate;
            Player.instance.transform.position += adjustedMovementVector;
            initialGripPosition = currentGripTransform.position;
            glideTimePassed += Time.deltaTime;
        }
        else if (isGliding)
        {
            magnitude -= momentumDrag * Time.deltaTime / player.transform.localScale.x;
            if (magnitude < 0) magnitude = 0;

            //Player.instance.transform.position += glidingVector * magnitude * Time.deltaTime;// / player.transform.localScale.x;
            //transform.position = Vector3.Lerp(transform.position, transform.position + glidingVector * magnitude, Time.deltaTime);
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
        isRightGripPressed = false;
        if (isScaling) return;

        if (isPanning && currentHand == fromSource)
        {
            isPanning = false;
            grabOffPosition = currentGripTransform.position;

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
                currentGripTransform = player.rightHand.panTransform;
                initialGripPosition = currentGripTransform.transform.position;
                isPanning = true;
                currentHand = fromSource;
            }
            isGliding = false;
            grabPosition = currentGripTransform.position;
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
            grabOffPosition = currentGripTransform.position;

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
                currentGripTransform = player.leftHand.panTransform;
                initialGripPosition = currentGripTransform.transform.position;
                isPanning = true;
                currentHand = fromSource;
            }

            isGliding = false;
            grabPosition = currentGripTransform.position;
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