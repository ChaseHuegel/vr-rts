using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Swordfish;

public class HealthBar : MonoBehaviour
{
    [SerializeField]
    public bool showBarBackground = true;
    public bool showBarForeground = true;
    public bool showText = true;

    [Range(0, 1.0f)]
    public float autoshowAt = 0.98f;

    [Range(0, 1.0f)]
    public float autohideAt = 1.0f;
    public Image healthBarBackgroundImage;
    public Image healthBarForegroundImage;
    public Text healthBarStatusText;
    private Damageable damageable;

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
            damageable = GetComponentInParent<Damageable>();

        if (damageable)
        {
            damageable.OnDamageEvent += OnDamage;
            damageable.OnHealthRegainEvent += OnHealthRegainEvent;
            SetFilledAmount(damageable.GetAttributePercent(Attributes.HEALTH));
        }
        else
        {
            Debug.Log("Damageable component not found in parent.", this);
        }
    }

    public void OnHealthRegainEvent(object sender, Damageable.HealthRegainEvent e)
    {
        if (damageable)
            SetFilledAmount(e.health / damageable.GetMaxHealth());
    }

    public void OnDamage(object sender, Damageable.DamageEvent e)
    {
        if (damageable)
            SetFilledAmount(damageable.GetHealth() - e.damage / damageable.GetMaxHealth());

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

        if (amount >= autohideAt)
            Hide();
        else if (amount < autoshowAt)
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
