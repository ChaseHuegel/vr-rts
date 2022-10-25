using System.Collections;
using System.Collections.Generic;
using Swordfish;
using UnityEngine;
using UnityEngine.UI;

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

    public bool isVisible { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        isVisible = false;

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
            SetFilledAmount(damageable.Attributes.ValueOf(AttributeConstants.HEALTH));
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
            SetFilledAmount((damageable.GetHealth() - e.damage) / damageable.GetMaxHealth());

        if (GetFilledAmount() <= 0.0f)
            ForceHide();

    }

    /// <summary>
    /// Get the health bar image filled amount.
    /// </summary>
    /// <returns>The amount the bar is filled, between 0 and 1</returns>
    public float GetFilledAmount()
    {
        return healthBarBackgroundImage.fillAmount;
    }

    /// <summary>
    /// Set the health bar image filled amount and the text display if enabled.
    /// </summary>
    /// <param name="amount">0.0f - 1.0f</param>
    public void SetFilledAmount(float amount)
    {
        if (healthBarForegroundImage)
            healthBarForegroundImage.fillAmount = amount;

        if (showText)
            healthBarStatusText.text = (((int)(amount * 100)).ToString()) + "%";

        if (amount >= autohideAt)
            Hide();
        else if (amount < autoshowAt)
            Show();

        if (amount <= 0.0f)
            ForceHide();
    }

    /// <summary>
    /// Hides the enabled health bar components unless the filled amount is less
    /// than the autoshowAt setting and greater than zero
    /// </summary>
    public void Hide()
    {
        float filledAmount = GetFilledAmount();
        if (filledAmount < autoshowAt && filledAmount > 0)
            return;

        if (showBarBackground)
            healthBarBackgroundImage.gameObject.SetActive(false);
        if (showBarForeground)
            healthBarForegroundImage.gameObject.SetActive(false);
        if (showText)
            healthBarStatusText.gameObject.SetActive(false);

        isVisible = false;
    }

    public void ForceHide()
    {
        healthBarBackgroundImage.gameObject.SetActive(false);
        healthBarForegroundImage.gameObject.SetActive(false);
        healthBarStatusText.gameObject.SetActive(false);
        isVisible = false;
    }

    public void TryShow()
    {
        if (GetFilledAmount() <= 0.0f)
        {
            ForceHide();
            return;
        }

        Show();
    }

    public void Show()
    {
        if (showBarBackground)
            healthBarBackgroundImage.gameObject.SetActive(true);
        if (showBarForeground)
            healthBarForegroundImage.gameObject.SetActive(true);
        if (showText)
            healthBarStatusText.gameObject.SetActive(true);

        isVisible = true;
    }
}
