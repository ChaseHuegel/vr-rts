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
    private Vector3 initialPosition;

    // Start is called before the first frame update
    void Start()
    {
        player = Valve.VR.InteractionSystem.Player.instance;
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
                initialHandDistance = Vector3.Distance(player.rightHand.transform.localPosition, player.leftHand.transform.localPosition);

                Vector3 midPoint = (player.rightHand.transform.position + player.leftHand.transform.position) * 0.5f;
                //Vector3 midPoint = Vector3.Lerp(player.rightHand.transform.position, player.leftHand.transform.position, 0.5f);
                
                initialPosition = midPoint;
                startScale = targetTransform.localScale.x;
                isScaling = true;
                isPanning = false;
                //Debug.Log("Scaling Start");
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

            float currentHandDistance = Vector3.Distance(player.leftHand.transform.localPosition, player.rightHand.transform.localPosition);
            float distanceDelta = (currentHandDistance - initialHandDistance);
            distanceDelta *= -scalingSensitivity;
            float newScale = startScale + (distanceDelta);
            float clampedNewScale = Mathf.Clamp(newScale, minScale, maxScale);
            targetTransform.localScale = new Vector3(clampedNewScale, clampedNewScale, clampedNewScale);
            
            // This method induces sickness...
            //ScaleAround(targetTransform.gameObject, initialPosition, new Vector3(clampedNewScale, clampedNewScale, clampedNewScale));

            //Debug.LogFormat("initHandDist= {0} : curHandDist= {1} : distDelta= {2} : startScale= {3} : clampNewScale= {4}", initialHandDistance, currentHandDistance, distanceDelta, startScale, clampedNewScale);

        }
        else if (isPanning)
        {
            movementVector = panStartPosition - panHandTransform.position;
            Player.instance.transform.position += movementVector * panMovementRate / targetTransform.localScale.x;
            panStartPosition = panHandTransform.position;
            glideTimePassed += Time.deltaTime;
        }
        else if (isGliding)
        {            
            magnitude -= momentumDrag * Time.deltaTime;

            if (magnitude < 0) magnitude = 0;

            Player.instance.transform.position += glidingVector * magnitude * Time.deltaTime;
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
                glidingVector = grabOffPosition - grabPosition;
                magnitude = (glidingVector.magnitude / glideTimePassed) * momentumStrength;
                isGliding = true;
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
                glidingVector = grabOffPosition - grabPosition;
                magnitude = (glidingVector.magnitude / glideTimePassed) * momentumStrength;
                isGliding = true;
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

    /// <summary>
    /// Scales the target around an arbitrary point by scaleFactor.
    /// This is relative scaling, meaning using  scale Factor of Vector3.one
    /// will not change anything and new Vector3(0.5f,0.5f,0.5f) will reduce
    /// the object size by half.
    /// The pivot is assumed to be the position in the space of the target.
    /// Scaling is applied to localScale of target.
    /// </summary>
    /// <param name="target">The object to scale.</param>
    /// <param name="pivot">The point to scale around in space of target.</param>
    /// <param name="scaleFactor">The factor with which the current localScale of the target will be multiplied with.</param>
    public static void ScaleAroundRelative(GameObject target, Vector3 pivot, Vector3 scaleFactor)
    {
        // pivot
        var pivotDelta = target.transform.localPosition - pivot;
        pivotDelta.Scale(scaleFactor);
        target.transform.localPosition = pivot + pivotDelta;

        // scale
        var finalScale = target.transform.localScale;
        finalScale.Scale(scaleFactor);
        target.transform.localScale = finalScale;
    }

    /// <summary>
    /// Scales the target around an arbitrary pivot.
    /// This is absolute scaling, meaning using for example a scale factor of
    /// Vector3.one will set the localScale of target to x=1, y=1 and z=1.
    /// The pivot is assumed to be the position in the space of the target.
    /// Scaling is applied to localScale of target.
    /// </summary>
    /// <param name="target">The object to scale.</param>
    /// <param name="pivot">The point to scale around in the space of target.</param>
    /// <param name="scaleFactor">The new localScale the target object will have after scaling.</param>
    public static void ScaleAround(GameObject target, Vector3 pivot, Vector3 newScale)
    {
        // pivot
        Vector3 pivotDelta = target.transform.localPosition - pivot; // diff from object pivot to desired pivot/origin
        Vector3 scaleFactor = new Vector3(
            newScale.x / target.transform.localScale.x,
            newScale.y / target.transform.localScale.y,
            newScale.z / target.transform.localScale.z);
        pivotDelta.Scale(scaleFactor);
        target.transform.localPosition = pivot + pivotDelta * 1.0f;

        //scale
        target.transform.localScale = newScale;
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