using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class VillagerHoverMenu : MonoBehaviour
{
    public float TimeDelayToHideMenu = 3.0f;
    public GameObject objectToToggle;
    private bool isVisible;
    private bool isHiding;
    private float timer;

    public void Show()
    {
        objectToToggle.SetActive(true); 
        isHiding = false;           
    }

    public void Hide()
    {        
        isHiding = true;
    }

    void Update()
    {
        if (isHiding)
        {
            timer += Time.deltaTime;
            if (timer >= TimeDelayToHideMenu)
            {
                objectToToggle.SetActive(false);
                timer = 0.0f;
                isHiding = false;
            }
        }
    }
}
