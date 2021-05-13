using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField]
    bool showBarBackground = true;
    [SerializeField]
    bool showBarForeground = true;
    [SerializeField]
    bool showText = true;
    [SerializeField]
    bool hideWhenFull = true;
    public Image healthBarBackgroundImage;
    public Image healthBarForegroundImage;
    public Text healthBarStatusText;

    // Start is called before the first frame update
    void Start()
    {
        if (!showText)
            healthBarStatusText.enabled = false;

        if (!showBarForeground)
            healthBarForegroundImage.enabled = false;

        if (!showBarBackground)
            healthBarBackgroundImage.enabled = false;

        gameObject.SetActive(true);

        if (healthBarForegroundImage.fillAmount >= 1.0f && hideWhenFull)
            gameObject.SetActive(false);
    }

    public float GetFilledAmount()
    {
        return healthBarBackgroundImage.fillAmount;
    }

    // Set fill amount, between 0 and 1
    public void SetFilledAmount(float amount)
    {
        if (showBarBackground)
            healthBarBackgroundImage.fillAmount = amount;

        if (showBarForeground)
            healthBarForegroundImage.fillAmount = amount;

        if (showText)
            healthBarStatusText.text = (((int)(amount * 100)).ToString()) + "%";

        if (amount >= 1.0f && hideWhenFull)
            gameObject.SetActive(false);            
    }
}
