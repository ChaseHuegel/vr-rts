using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Valve.VR.InteractionSystem;
using UnityEngine.Events;

public class BuildingHoverDisplay : MonoBehaviour
{
    public string title;

    [Header("Options")]
    public bool startHidden = true;
    public GameObject titleGameObject;
    public GameObject menuGameObject;
    public GameObject healthBarGameObject;

    protected LookAtAndReset lookAtAndReset;
    protected bool visible;
    public GameObject buildingHoverDisplayGameObject;

    // Start is called before the first frame update
    void Start()
    {
        lookAtAndReset = buildingHoverDisplayGameObject.GetComponentInChildren<LookAtAndReset>();

        if (titleGameObject)
        {
            TextMeshPro titleText = titleGameObject.GetComponentInChildren<TextMeshPro>();
            if (titleText)
            {
                titleText.text = title;
            }
            else
                Debug.Log("Missing textmeshpro component in child objects.");

            // if (menuGameObject)
            //     titleGameObject.transform.localPosition = new Vector3(0, 1.2f, 0);
            // else
            //     titleGameObject.transform.localPosition = new Vector3(0, 0.4f, 0);
        }
        else
            Debug.Log("Missing titleGameObject.");

        if (startHidden)
            Hide();
        else
            Show();

        
    }
    
    protected float firstKnockTime;
    protected float secondKnockMaxDuration = 1.0f;
    protected bool waitingForSecondKnock;

    
    private void HandHoverUpdate( Hand hand )
    {
        // We're making a fist
        if (hand.IsGrabbingWithType(GrabTypes.Pinch) && hand.IsGrabbingWithType(GrabTypes.Grip))
        {
            
        }    
        //AudioSource.PlayClipAtPoint(GameMaster.GetAudio("knock").GetClip(), transform.position);
       
    }

    public void OnHandHoverBegin(Hand hand)
    {        
        if (hand.IsGrabbingWithType(GrabTypes.Pinch) && hand.IsGrabbingWithType(GrabTypes.Grip))
        {
            Debug.Log("fist");
        }    
        //Debug.Log("hovering");

        // if (Player.instance.leftHand.hoveringInteractable == GetComponentInParent<Interactable>())
        // {
            // GrabTypes grabType = Player.instance.leftHand.GetGrabStarting();
            // Debug.Log(grabType);
        // }

        // Check if hand pose is a fist and play knock if it is.
        // GrabTypes grabType = hand.GetBestGrabbingType();
        // Debug.Log(grabType);
        // AudioSource.PlayClipAtPoint(GameMaster.GetAudio("knock").GetClip(), transform.position);
        // Toggle();
        
        // ---- 2 knocks is too unreliable at the moment and can deal with it later
        // if (waitingForSecondKnock)
        // {
        //     // This is the 2nd knock
        //     if (Time.fixedTime - firstKnockTime <= secondKnockMaxDuration)
        //     {
        //         Toggle();
        //         waitingForSecondKnock = false;
        //         Debug.Log("second " + (Time.fixedTime - firstKnockTime).ToString());
        //     }
        //     // Time windows has passed for 2nd knock
        //     else
        //     {
        //         waitingForSecondKnock = false;                
        //     }
        // }
        // // This is a new first knock
        // else
        // {
        //     firstKnockTime = Time.fixedTime;
        //     waitingForSecondKnock = true;
        //     Debug.Log("first " + firstKnockTime);
        // }

        //Debug.Log("Hover Begin");
    }

    public void Toggle()
    {
        if (visible)
            Hide();
        else
            Show();
    }

    public void HideMenu() 
    { 
        if (menuGameObject)
            menuGameObject.SetActive(false); 
    }

    public void HideHealthBar() 
    { 
        if (healthBarGameObject)
            healthBarGameObject.SetActive(false); 
        
        lookAtAndReset.enabled = false;
    }

    public void HideTitle() 
    { 
        if (titleGameObject)
            titleGameObject.SetActive(false); 
    }

    public void ShowMenu() 
    { 
        if (menuGameObject)
            menuGameObject.SetActive(true); 
    }

    public void ShowHealthBar() 
    { 
        if (healthBarGameObject)
            healthBarGameObject.SetActive(true); 

        lookAtAndReset.enabled = true;
    }
    
    public void ShowTitle() 
    { 
        if (titleGameObject)
            titleGameObject.SetActive(true); 
    }
    
    public void Hide() 
    { 
        HideMenu();
        HideTitle();
        HideHealthBar();
        visible = false;
        lookAtAndReset.enabled = false;
    }
    
    public void Show() 
    {
        ShowMenu(); 
        ShowTitle(); 
        ShowHealthBar();
        visible = true;
        lookAtAndReset.enabled = true;
    }
    
}
