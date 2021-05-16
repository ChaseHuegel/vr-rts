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
    public bool autohide = true;
    public float autohideDelay = 15.0f;
    public float autohideRadius = 2.0f;
    public GameObject titleGameObject;
    public GameObject menuGameObject;
    public HealthBar healthBar;

    protected LookAtAndReset lookAtAndReset;
    protected bool visible;
    public GameObject buildingHoverDisplayGameObject;
    protected float lastKnockTime;
    protected float secondKnockMaxDuration = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        if (!(lookAtAndReset = buildingHoverDisplayGameObject.GetComponentInChildren<LookAtAndReset>()))
            Debug.Log("LookAtAndReset component not found.", this);

        lookAtAndReset.SetAutohideParameters(autohide, autohideDelay, autohideRadius);

        // TODO: Set this reference in all buildings for performance gains.
        healthBar = GetComponentInChildren<HealthBar>(true);

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
        
        healthBar.enabled = true;

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
            {
                Toggle();
            }
            
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

    public void HideMenu() 
    { 
        if (menuGameObject)
            menuGameObject.SetActive(false); 
    }

    public void HideHealthBar() 
    { 
        if (healthBar.GetFilledAmount() < 1.0f)
        {
            healthBar.gameObject.SetActive(true); 
            lookAtAndReset.enabled = true;
        }
        else
        {
            healthBar.gameObject.SetActive(false);
            lookAtAndReset.enabled = false;
        }

        
    }
    
    public void Hide() 
    {     
        lookAtAndReset.enabled = false;
        HideMenu();
        HideTitle();
        HideHealthBar();
        visible = false;
        
    }
    
    public void Show() 
    {
        lookAtAndReset.enabled = true;
        ShowMenu(); 
        ShowTitle(); 
        ShowHealthBar();
        visible = true;        
    }
    
    
    private void HideTitle() 
    { 
        if (titleGameObject)
            titleGameObject.SetActive(false); 
    }

    private void ShowMenu() 
    { 
        if (menuGameObject)
            menuGameObject.SetActive(true); 
    }

    private void ShowHealthBar() 
    { 
        if (healthBar)
        {
            healthBar.gameObject.SetActive(true); 
            lookAtAndReset.enabled = true;
        }
    }
    
    private void ShowTitle() 
    { 
        if (titleGameObject)
            titleGameObject.SetActive(true); 
    }
}
