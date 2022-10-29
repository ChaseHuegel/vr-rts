using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingInteractionPanel : MonoBehaviour
{
    [Header("Title Display")]
    public string title = "No Title Set";
    public bool enableTitleDislpay = true;
    public float titleDisplayVerticalOffset = -0.5f;
    public Color titleColor = Color.white;

    [Header("Health Bar Display")]
    public bool enableHealthBarDisplay = true;
    public bool showBarBackground = true;
    public bool showHealthText = true;
    public float healthBarVerticalOffset = 0.5f;
    public float healthBarWidth = 1.0f;
    public float healthBarHeight = 0.1f;
    public Sprite healthBarBackground;
    public Color healthBarBackgroundColor = Color.black;
    public Sprite healthBarForeground;
    public Color healthBarForegroundColor = Color.red;
    public Color healthBarTextColor = Color.white;
    private GameObject titleGameObject;
    private GameObject healthBarGameObject;
    private Image healthBarBackgroundImage;
    private Image healthBarForegroundImage;
    private TextMeshPro healthBarText;

    // Start is called before the first frame update
    void Start()
    {
        if (enableTitleDislpay)
            InitializeTitleDisplay();

        if (enableHealthBarDisplay)
            InitializeHealthBarDisplay();

        // if (damageable)
        // {
        //     damageable.OnDamageEvent += OnDamage;
        //     damageable.OnHealthRegainEvent += OnHealthRegainEvent;
        //     SetFilledAmount(damageable.Attributes.ValueOf(AttributeConstants.HEALTH));
        // }
        // else
        // {
        //     Debug.Log("Damageable component not found in parent.", this);
        // }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InitializeTitleDisplay()
    {
        titleGameObject = new GameObject("_building_title");
        titleGameObject.transform.position = new Vector3(0.0f, titleDisplayVerticalOffset, 0.0f);
        titleGameObject.transform.SetParent(this.gameObject.transform, false);

        TextMeshPro titleText = titleGameObject.AddComponent<TextMeshPro>();
        titleText.SetText(title);
        titleText.fontStyle = FontStyles.Bold;
        titleText.fontSize = 1.0f;
        titleText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        titleText.color = titleColor;
        titleText.raycastTarget = false;
    }

    private void InitializeHealthBarDisplay()
    {
        healthBarGameObject = new GameObject("_health_bar");
        healthBarGameObject.transform.position = new Vector3(0.0f, healthBarVerticalOffset, 0.0f);
        healthBarGameObject.transform.SetParent(this.gameObject.transform, false);

        if (showBarBackground)
        {
            GameObject healthBarBackroundGameObject = new GameObject("_health_bar_background");
            healthBarBackroundGameObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            healthBarBackroundGameObject.transform.SetParent(healthBarGameObject.transform, false);

            healthBarBackroundGameObject.AddComponent<Canvas>().sortingOrder = 0;
            healthBarBackgroundImage = healthBarBackroundGameObject.AddComponent<Image>();
            healthBarBackroundGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
            healthBarBackgroundImage.sprite = healthBarBackground;
            healthBarBackgroundImage.color = healthBarBackgroundColor;
        }

        GameObject healthBarForegroundGameObject = new GameObject("_health_bar_foreground");
        healthBarForegroundGameObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        healthBarForegroundGameObject.transform.SetParent(healthBarGameObject.transform, false);

        healthBarForegroundGameObject.AddComponent<Canvas>().sortingOrder = 1;
        healthBarForegroundImage = healthBarForegroundGameObject.AddComponent<Image>();
        healthBarForegroundGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
        healthBarForegroundImage.sprite = healthBarForeground;
        healthBarForegroundImage.type = Image.Type.Filled;
        healthBarForegroundImage.fillMethod = Image.FillMethod.Horizontal;
        healthBarForegroundImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthBarForegroundImage.raycastTarget = false;
        healthBarForegroundImage.color = healthBarForegroundColor;

        if (showHealthText)
        {
            GameObject healthBarTextObject = new GameObject("_health_bar_text");

            healthBarTextObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            healthBarTextObject.transform.SetParent(healthBarGameObject.transform, false);
            healthBarTextObject.AddComponent<Canvas>();

            healthBarText = healthBarTextObject.AddComponent<TextMeshPro>();
            healthBarText.SetText(title);
            healthBarText.fontStyle = FontStyles.Bold;
            healthBarText.fontSize = 0.8f;
            healthBarText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            healthBarText.verticalAlignment = VerticalAlignmentOptions.Middle;
            healthBarText.raycastTarget = false;
            healthBarText.sortingOrder = 2;
            healthBarText.color = healthBarTextColor;
            
            SetHealthBarFilledAmount(0.75f);
        }

    }

    private void SetHealthBarFilledAmount(float amount)
    {
        if (healthBarForeground)
            healthBarForegroundImage.fillAmount = amount;

        if (showHealthText)
            healthBarText.text = (((int)(amount * 100)).ToString()) + "%";

        // if (amount >= autohideAt)
        //     Hide();
        // else if (amount < autoshowAt)
        //     Show();

        // if (amount <= 0.0f)
        //     ForceHide();
    }
}
