using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swordfish;

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

    public Damageable damageable;

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

        if (!damageable)
        {
            damageable = GetComponentInParent<Damageable>();
            if (damageable)
            {
                damageable.OnDamageEvent += OnDamage;
                SetFilledAmount(damageable.GetAttributePercent(Attributes.HEALTH));
            }
        }
    }

    public void OnDamage(object sender, Damageable.DamageEvent e)
    {
        if (damageable)
            SetFilledAmount(damageable.GetAttributePercent(Attributes.HEALTH));

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
            Hide();
        else if (amount < 1.0f)
            Show();
    }

    public void Hide()
    {   
        if (showBarBackground)
            healthBarBackgroundImage.gameObject.SetActive(false);
        if (showBarForeground)
            healthBarForegroundImage.gameObject.SetActive(false);
        if (showText)
            healthBarStatusText.gameObject.SetActive(false);
    }
    public void Show()
    {   
        if (showBarBackground)
            healthBarBackgroundImage.gameObject.SetActive(true);
        if (showBarForeground)
            healthBarForegroundImage.gameObject.SetActive(true);
        if (showText)
            healthBarStatusText.gameObject.SetActive(true);
    }
}
