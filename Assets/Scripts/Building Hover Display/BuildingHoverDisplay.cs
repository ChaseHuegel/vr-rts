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
    public HealthBar healthBar;

    protected LookAtAndReset lookAtAndReset;
    protected bool visible;
    public GameObject buildingHoverDisplayGameObject;
    protected float lastKnockTime;
    protected float secondKnockMaxDuration = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        lookAtAndReset = buildingHoverDisplayGameObject.GetComponentInChildren<LookAtAndReset>();
        
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
            healthBar.gameObject.SetActive(true); 
        else
            healthBar.gameObject.SetActive(false);

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
        if (healthBar)
            healthBar.gameObject.SetActive(true); 
        
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
