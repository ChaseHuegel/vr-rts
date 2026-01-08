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
    public float minScale = 1.0f; // 

    [Tooltip("Maximum world scale of transform.")]
    public float maxScale = 5.0f; // 

    [Tooltip("Sensitivity and inversion of movement required.")]
    public float scalingSensitivity = 10.0f; // 

    [Header("Grip Panning")]
    public SteamVR_Action_Boolean GripOnOff;
    public float floorHeight = 0f;
    public float panSensitivity = 1.0f; // Movement rate multiplier
    public bool useMomentum = true;
    public float glideStrength = 3.0f;
    public float glideDrag = 5.0f;
    [Tooltip("Minimum world distance the player must have moved during a pan to enable glide")]
    public float minGlideDistance = 0.05f;
    [Tooltip("Minimum average speed (world units / sec) during the pan to enable glide")]
    public float minGlideSpeed = 0.1f;

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
    private Vector3 startPlayerPosition;
    private Vector3 accumulatedAppliedDelta;
    float glideMagnitude;
    bool isRightHandPanEnabled = true;
    bool isLeftHandPanEnabled = true;
    public bool IsScalingEnabled() => isRightHandPanEnabled && isLeftHandPanEnabled;
    private bool isRightGripPressed => GripOnOff.GetState(SteamVR_Input_Sources.RightHand);
    private bool isLeftGripPressed => GripOnOff.GetState(SteamVR_Input_Sources.LeftHand);
    private bool isScalingStarting() => isRightGripPressed && isLeftGripPressed && !isScaling;
     private float startScale = 1.0f;
    private float initialHandDistance;
    [Tooltip("Dot product threshold to consider palms facing each other (0..1)")]
    public float palmFacingThreshold = 0.5f;

    private bool PalmsFacingEachOther()
    {
        var right = Player.instance?.rightHand;
        var left = Player.instance?.leftHand;
        if (right == null || left == null)
            return false;

        Vector3 vec = left.transform.position - right.transform.position;
        if (vec.sqrMagnitude < 1e-6f)
            return false;

        Vector3 dir = vec.normalized;

        // Check that each hand's palm normal (up) is pointing roughly toward the other hand.
        // Use transform.up as the palm normal; flip sign if your hand model uses -up for palms
        float rightDot = Vector3.Dot(right.transform.right, -dir);
        float leftDot = Vector3.Dot(left.transform.right, -dir);

        return rightDot >= palmFacingThreshold && leftDot >= palmFacingThreshold;
    }

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
        // Start scaling only when both grips pressed and palms face each other
        if (!isScaling && IsScalingEnabled() && isRightGripPressed && isLeftGripPressed && PalmsFacingEachOther())
        {
            // ensure we reference the correct hand instances
            currentHand = Player.instance.rightHand;
            otherHand = Player.instance.leftHand;
            initialHandDistance = Mathf.Abs(currentHand.transform.localPosition.x - otherHand.transform.localPosition.x);
            startScale = targetTransform.localScale.x;
            Debug.Log("Starting scaling at scale: " + startScale + " hand distance: " + initialHandDistance);
            isScaling = true;
            isPanning = false;
        }

        if (isScaling && IsScalingEnabled())
        {
            // Stop scaling if either grip released or palms are no longer facing each other
            if (!isRightGripPressed || !isLeftGripPressed || !PalmsFacingEachOther())
            {
                isScaling = false;
                Debug.Log("Ending scaling at scale: " + targetTransform.localScale.x);
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
            Vector3 adjustedMovementVector = movementVector * panSensitivity;
            Player.instance.transform.position += adjustedMovementVector;
            // track the actual applied world movement so glide uses the same displacement
            accumulatedAppliedDelta += adjustedMovementVector;
            initialGripPosition = currentHand.panTransform.position;
            glideTimePassed += Time.deltaTime;
        }
        else if (isGliding)
        {
            // Apply current velocity (glideMagnitude is in units per second)
            if (glideMagnitude > 0.0001f)
            {
                Player.instance.transform.position += glidingVector * glideMagnitude * Time.deltaTime;

                // smooth exponential decay for less jerky feel (frame-rate independent)
                float decayFactor = Mathf.Exp(-glideDrag * Time.deltaTime);
                glideMagnitude *= decayFactor;

                if (glideMagnitude <= 0.001f)
                {
                    glideMagnitude = 0f;
                    isGliding = false;
                }
                else
                {
                    Debug.Log("Gliding with magnitude: " + glideMagnitude);
                }
            }
            else
            {
                glideMagnitude = 0f;
                isGliding = false;
            }
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
            // If the other hand is holding its grip and can pan, transfer control to it instead of stopping
            if (isLeftGripPressed && isLeftHandPanEnabled && Player.instance.leftHand.hoveringInteractable == null)
            {
                currentHand = Player.instance.leftHand;
                otherHand = Player.instance.rightHand;
                // Reset pan tracking for new hand
                initialGripPosition = currentHand.panTransform.position;
                startGrabPosition = currentHand.panTransform.position;
                startPlayerPosition = Player.instance.transform.position;
                accumulatedAppliedDelta = Vector3.zero;
                glideTimePassed = 0.0f;
                isGliding = false;
                // keep isPanning = true
            }
            else
            {
                isPanning = false;

                // compute glide from player-world movement during the pan so it's independent of panSensitivity
                Vector3 endPlayerPosition = Player.instance.transform.position;

                if (useMomentum && glideTimePassed > 0.0001f)
                {
                    Vector3 worldDelta = accumulatedAppliedDelta; // use actual applied movement
                    float travel = worldDelta.magnitude;
                    float avgSpeed = travel / Mathf.Max(glideTimePassed, 0.0001f);

                    // Only enable glide if movement exceeded configurable thresholds to avoid accidental glides
                    if (travel >= minGlideDistance && avgSpeed >= minGlideSpeed)
                    {
                        isGliding = true;
                        glidingVector = worldDelta.normalized;
                        glideMagnitude = (travel / Mathf.Max(glideTimePassed, 0.0001f)) * glideStrength;
                    }
                }

                currentHand = null;
            }
        }
    }

    public void OnRightGripPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (isRightHandPanEnabled && !isScaling)
        {
            Hand newHand = Player.instance.rightHand;
            if (newHand.hoveringInteractable == null)
            {
                // allow takeover if other hand was panning
                currentHand = newHand;
                otherHand = Player.instance.leftHand;
                initialGripPosition = currentHand.panTransform.position;
                isPanning = true;

                // reset glide tracking
                isGliding = false;
                startGrabPosition = currentHand.panTransform.position;
                startPlayerPosition = Player.instance.transform.position;
                glideTimePassed = 0.0f;
                accumulatedAppliedDelta = Vector3.zero;
            }
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
            // If the other hand is holding its grip and can pan, transfer control to it instead of stopping
            if (isRightGripPressed && isRightHandPanEnabled && Player.instance.rightHand.hoveringInteractable == null)
            {
                currentHand = Player.instance.rightHand;
                otherHand = Player.instance.leftHand;
                // Reset pan tracking for new hand
                initialGripPosition = currentHand.panTransform.position;
                startGrabPosition = currentHand.panTransform.position;
                startPlayerPosition = Player.instance.transform.position;
                accumulatedAppliedDelta = Vector3.zero;
                glideTimePassed = 0.0f;
                isGliding = false;
                // keep isPanning = true
            }
            else
            {
                isPanning = false;

                Vector3 endPlayerPosition = Player.instance.transform.position;

                if (useMomentum && glideTimePassed > 0.0001f)
                {
                    Vector3 worldDelta = accumulatedAppliedDelta;
                    float travel = worldDelta.magnitude;
                    float avgSpeed = travel / Mathf.Max(glideTimePassed, 0.0001f);

                    if (travel >= minGlideDistance && avgSpeed >= minGlideSpeed)
                    {
                        isGliding = true;
                        glidingVector = worldDelta.normalized;
                        glideMagnitude = (travel / Mathf.Max(glideTimePassed, 0.0001f)) * glideStrength;
                    }
                }

                currentHand = null;
            }
        }
    }

    public void OnLeftGripPressed(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (isLeftHandPanEnabled && !isScaling)
        {
            Hand newHand = Player.instance.leftHand;
            if (newHand.hoveringInteractable == null)
            {
                currentHand = newHand;
                otherHand = Player.instance.rightHand;
                initialGripPosition = currentHand.panTransform.position;
                isPanning = true;

                isGliding = false;
                startGrabPosition = currentHand.panTransform.position;
                startPlayerPosition = Player.instance.transform.position;
                glideTimePassed = 0.0f;
                accumulatedAppliedDelta = Vector3.zero;
            }
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