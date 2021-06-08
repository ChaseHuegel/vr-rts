using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Valve.VR.InteractionSystem;
using UnityEngine.Events;

public class BuildingHoverDisplay : MonoBehaviour
{        
    [Header("Options")]
    // TODO: title should be pulled from database through constructable/structure buildingData
    public string title;
    public bool startHidden = true;
    public bool autohide = true;
    public bool menuEnabled = false;

    [Tooltip("Delay to hide the menu after the target has crossed the autohideDistance threshold.")]
    public float autohideDelay = 15.0f;
    
    [Tooltip("Distance required from object to target before the autohide timer starts.")]
    public float autohideDistance = 2.0f;
    public AutohideBillboard autoHideBillboard;
    public GameObject titleGameObject;
    public GameObject menuGameObject;
    public HealthBar healthBar;
  
    public GameObject[] objectsToAutohide;
    
    protected bool visible;
    protected float lastKnockTime;
    protected float secondKnockMaxDuration = 0.5f;

    void Start()
    {
        if (!autoHideBillboard && !(autoHideBillboard = GetComponentInChildren<AutohideBillboard>()))
            Debug.Log("AutoHideBillboard not found.", this);
            
        else if (autoHideBillboard)        
        {
            autoHideBillboard.SetAutohideParameters(true, false, autohideDelay, autohideDistance);
            autoHideBillboard.onAutoHide += Hide;
        }

        // Set the title of the display.
        if (!titleGameObject)
            Debug.Log("titleGameObject not set.", this);
        else
        {
            TextMeshPro titleText = titleGameObject.GetComponentInChildren<TextMeshPro>();
            if (titleText)
                titleGameObject.GetComponentInChildren<TextMeshPro>().text = title;
            else
                Debug.Log("Missing TextMeshPro component in titleGameObject children.", this);
        }

        // TODO: Set this reference in all buildings for performance gains.
        if (!healthBar && !(healthBar = GetComponentInChildren<HealthBar>()))
            Debug.Log("Healthbar component not found.", this);
        else if (healthBar)
        {
            healthBar = GetComponentInChildren<HealthBar>(true);        
            healthBar.enabled = true;
        }

        if (!menuGameObject && menuEnabled)
        {
            Debug.Log("menuGameObject not set.", this);
            menuEnabled = false;
        }
        else if (menuGameObject && !menuEnabled)
            menuGameObject.SetActive(false);
            

        if (startHidden)
            Hide();
        else
            Show();
    }
    
    public void OnHandHoverBegin(Hand hand)
    {        
        // Make sure we are making a fist
        if (hand.IsGrabbingWithType(GrabTypes.Pinch) && hand.IsGrabbingWithType(GrabTypes.Grip))
        {
            AudioSource.PlayClipAtPoint(GameMaster.GetAudio("knock").GetClip(), transform.position);
                        
            if (Time.fixedTime - lastKnockTime <= secondKnockMaxDuration)
                Toggle();
            
            lastKnockTime = Time.fixedTime;
        }
    }

    public void Toggle()
    {
        if (visible)
            Hide();
        else
            Show();
    }
    
    public void Hide() 
    {     
        visible = false;
        titleGameObject.SetActive(false); 
        if (menuEnabled) menuGameObject?.SetActive(false); 
            
        // TODO: Should be based on the healtbars autoshowAt/autohideAt values.
        // TODO: Change to healthbar events that autohideBillboard can subscribe to.
        if (healthBar.isVisible)
            autoHideBillboard.enabled = true;
        else
            autoHideBillboard.enabled = false;

        healthBar.Hide();

        foreach (GameObject go in objectsToAutohide)
        {
            go.SetActive(false);
        }        
    }
    
    public void Show() 
    {
        visible = true;  
        titleGameObject.SetActive(true); 
        autoHideBillboard.enabled = true;

        if (menuEnabled) menuGameObject?.SetActive(true); 
        
        healthBar.TryShow();

        foreach (GameObject go in objectsToAutohide)
        {
            go.SetActive(true);
        }
    }
}
