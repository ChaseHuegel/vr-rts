using System.Collections.Generic;
using Swordfish;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable), typeof(Collider))]
public class BuildingInteractionPanel : MonoBehaviour
{
    [Header("Panel Behaviour")]
    public Vector3 panelScale = new Vector3(1.0f, 1.0f, 1.0f);
    public Vector3 panelPositionOffset = new Vector3(0.0f, 1.0f, 0.0f);
    public bool startHidden = true;

    [Tooltip("Autohide events are fired at the requisite time to notify listeners for external control of hiding.")]
    public bool autoHide = true;

    [Tooltip("Disable interaction panel child object when onAutoHide event is called.")]
    public bool disableSelf = true;

    [Tooltip("Time until the panel hides itself. If useDistance is enabled the timer doesn't start until player is outside the range set by useDistance.")]
    public bool useTimer = true;

    [Tooltip("Distance between player and panel required for the panel to hide itself. If useTimer is enabled then the timer won't start until the player is outside the range set by useDistance.")]
    public bool useDistance = true;

    [Tooltip("Delay to hide the menu after the target has crossed the autohideDistance threshold.")]
    public float autoHideDelay = 15.0f;

    [Tooltip("Distance required from object to target before the autohide timer starts.")]
    public float autoHideDistance = 2.0f;

    [Tooltip("Distance at which the billboard will stop rotating to face the target.")]
    public float stopRotatingAtDistance = 0.75f;

    [Tooltip("The speed used when rotating to face the target.")]
    public float rotationSpeed = 5.0f;
    public bool copyHmdRotation = true;
    public GameObject[] objectsToAutohide;

    [Header("Title Settings")]
    [Tooltip("Leave empty to use title from structure component.")]
    public string title;
    public bool enableTitleDisplay = true;
    public float titleDisplayVerticalOffset = 0.075f;
    public Color titleColor = Color.white;

    [Header("Health Bar Settings")]
    public bool enableHealthBarDisplay = true;
    [Range(0, 1.0f)]
    public float healthBarAutoShowAt = 0.98f;

    [Range(0, 1.0f)]
    public float healthBarAutoHideAt = 1.0f;
    public bool showBarBackground = true;
    public bool showHealthText = true;
    public float healthBarVerticalOffset = -0.075f;
    public float healthBarWidth = 1.0f;
    public float healthBarHeight = 0.1f;

    // TODO: Optimize - Move generation functions to a seperate class that can
    // be unloaded once the menus are generated.

    [Header("Queue Menu Settings")]
    public bool enableQueueMenu;
    public float QueueMenuVerticalOffset;
    public Transform unitSpawnPoint;
    public Transform unitRallyWaypoint;

    [Header("Queue Menu Button Settings")]    
    private List<TechBase> queueTechButtons;
    private float buttonSize = 0.25f;
    private float spaceBetweenButtons = 0.025f;
    private int maxButtonColumns = 5;

    [Header("Queue Menu Queue Slot Settings")]
    public byte numberOfQueueSlots = 12;
    private float queueSlotSize = 1.0f;
    private float spaceBetweenQueueSlots = 0.025f;
    private UnityEngine.UI.Image[] queueSlotImages;
    private Vector3 nextButtonPosition;

    private GameObject interactionPanelObject;
    private GameObject titleGameObject;
    private GameObject healthBarGameObject;
    private GameObject menuGameObject;
    private GameObject buttonsGameObject;
    private Image healthBarBackgroundImage;
    private Image healthBarForegroundImage;
    private TextMeshPro healthBarText;
    private float radiusExitTime;
    private bool autohideTimerStarted;
    private Transform faceTarget;
    private Vector3 progressPosition;
    private Vector3 cancelButtonPosition;
    private SpawnQueue techQueue;
    private Damageable damageable;
    private GameObject cancelButtonGameObject;
    private bool isVisible;

    private List<QueueUnitButton> queueUnitButtons;
    private BuildingData buildingData;

    private bool canQueueUnits = false;

    void Awake()
    {
        if (!faceTarget) faceTarget = Player.instance.hmdTransform;
    }

    // Start is called before the first frame update
    void Start()
    {
        Structure structure = GetComponent<Structure>();
        if (structure)
            buildingData = GetComponent<Structure>().buildingData;
        else
        {
            FactionedResource factionedResource = GetComponent<FactionedResource>();
            if (factionedResource)
                buildingData = factionedResource.buildingData;
        }

        if (buildingData.techQueueButtons.Count > 0)
        {
            enableQueueMenu = true;
            queueTechButtons = new List<TechBase>(buildingData.techQueueButtons);
        }
        else
            enableQueueMenu = false;

        if (interactionPanelObject)
            return;

        InitializeInteractionPanel();

        if (title == "")
            title = this.gameObject.GetComponent<Structure>()?.buildingData.title;

        if (title == "")
            title = this.gameObject.GetComponent<Constructible>()?.buildingData.title;

        if (enableTitleDisplay)
            InitializeTitleDisplay();

        if (enableHealthBarDisplay)
            InitializeHealthBarDisplay();

        if (enableQueueMenu)
        {
            techQueue = this.gameObject.AddComponent<SpawnQueue>();
            if (!techQueue)
                Debug.LogWarning("Missing SpawnQueue component.", this);

            InitializeMenuDisplay();
            HookIntoEvents();
        }

        // TODO: Probably should be handled differently
        PlayerManager.Instance.faction.techTree.RefreshNodes();

        if (startHidden)
            Hide();
        else
            Show();
    }

    private void OnNodeResearched(TechNode node)
    {
        // Do not destroy buttons if node is a unit node
        if (node.tech is UnitData)
            return;

        DestroyQueueButton(node);        
    }

    GameObject queueSlotsGameObject;

    private void DestroyQueueButton(TechNode techNode)
    {
        queueTechButtons.Remove(techNode.tech);

        if (techNode is ResearchNode)
        {
            ResearchNode researchNode = ((ResearchNode)techNode);
            foreach (TechNode targetNode in researchNode.targetNodes)
                queueUnitButtons.Find(x => x.techToQueue == targetNode.tech)?.gameObject.SetActive(true);
        }

        QueueUnitButton queueUnitButton = queueUnitButtons.Find(x => x.techToQueue == techNode.tech);
        queueUnitButtons?.Remove(queueUnitButton);
        Destroy(queueUnitButton?.gameObject);

        if (queueUnitButtons.Count > 0)
            RepositionQueueButtons();
        else
        {
            queueSlotsGameObject.SetActive(false);
            Destroy(queueSlotsGameObject);
            titleGameObject.transform.position = new Vector3(0.0f, titleDisplayVerticalOffset, 0.0f);
        }
    }

    private void HookIntoEvents()
    {
        TechTree.OnNodeResearched += OnNodeResearched;

    }

    private void CleanupEvents()
    {
        TechTree.OnNodeResearched -= OnNodeResearched;

        if (enableQueueMenu)
        {
            if (cancelButtonGameObject)
                cancelButtonGameObject.GetComponentInChildren<HoverButton>().onButtonDown.RemoveListener(OnCancelQueueHoverButtonDown);

            if (buttonsGameObject)
                foreach (HoverButton button in buttonsGameObject.GetComponentsInChildren<HoverButton>())
                {
                    button.onButtonDown.RemoveListener(OnQueueHoverButtonDown);
                    button.onButtonUp.RemoveListener(OnQueueHoverButtonUp);
                }
        }
    }

    void OnDestroy()
    {
        CleanupEvents();
    }

    private void InitializeInteractionPanel()
    {
        interactionPanelObject = new GameObject("_interaction_panel");
        interactionPanelObject.transform.SetParent(this.gameObject.transform, false);
        interactionPanelObject.transform.localPosition = panelPositionOffset;
        interactionPanelObject.transform.localScale = panelScale;

        radiusExitTime = Time.time;
        Quaternion rot = faceTarget.transform.rotation;
        rot.z = rot.x = 0;
        interactionPanelObject.transform.rotation = rot;

        Interactable interactable = GetComponent<Interactable>();
        interactable.TryAddToHideHighlight(interactionPanelObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (!faceTarget)
            return;

        if (!copyHmdRotation)
        {
            float distance = Vector3.Distance(faceTarget.position, transform.position);
            if (distance > stopRotatingAtDistance)
            {
                Vector3 t = (faceTarget.position - transform.position);
                t.y = 0;
                Quaternion rot = Quaternion.LookRotation(t);
                interactionPanelObject.transform.rotation = Quaternion.Slerp(interactionPanelObject.transform.rotation, rot, Time.deltaTime * rotationSpeed);
            }
        }
        else
        {
            float distance = Vector3.Distance(faceTarget.position, transform.position);
            if (distance > stopRotatingAtDistance)
            {
                //Vector3 t = (faceTarget.position - transform.position);
                //t.y = 0;
                //Quaternion rot = Quaternion.LookRotation(t);
                Quaternion rot = faceTarget.transform.rotation;// Quaternion.LookRotation(t);
                rot.z = rot.x = 0;
                interactionPanelObject.transform.rotation = Quaternion.Slerp(interactionPanelObject.transform.rotation, rot, Time.deltaTime * rotationSpeed);
            }
        }

        if (!autoHide)
            return;

        if (autohideTimerStarted)
        {
            if (Time.time - radiusExitTime >= autoHideDelay)
            {
                if (onAutoHide != null)
                    onAutoHide();

                Hide();

                autohideTimerStarted = false;
            }
        }
        else
        {
            float distance = (faceTarget.transform.position - transform.position).magnitude;
            if (distance >= autoHideDistance)
            {
                autohideTimerStarted = true;
                radiusExitTime = Time.time;
            }
        }
    }

    private void InitializeMenuDisplay()
    {
        menuGameObject = new GameObject("_menu");
        menuGameObject.transform.position = new Vector3(0.0f, QueueMenuVerticalOffset, 0.0f);
        menuGameObject.transform.SetParent(interactionPanelObject.transform, false);

        InitializeQueueButtons();

        queueSlotsGameObject = new GameObject("_queue_slots");
        queueSlotsGameObject.transform.SetParent(menuGameObject.transform, false);
        InitializeQueueSlots(queueSlotsGameObject.transform, 7, spaceBetweenQueueSlots);

        GameObject progressGameObject = new GameObject("_progress");
        progressGameObject.transform.SetParent(menuGameObject.transform, false);
        Vector2 size = new Vector2(queueSlotSize * 2.0f + spaceBetweenQueueSlots, queueSlotSize);
        InitializeProgressBar(progressPosition, size, 0.03f, progressGameObject.transform);

        cancelButtonGameObject = new GameObject("_cancel");
        cancelButtonGameObject.transform.SetParent(menuGameObject.transform, false);
        cancelButtonGameObject.transform.localPosition = cancelButtonPosition;
        cancelButtonGameObject.transform.localScale = new Vector3(buttonSize, buttonSize, buttonSize);        
        InitializeCancelButton(cancelButtonGameObject.transform);

        // Interactable interactable = GetComponent<Interactable>();
        // interactable.TryAddToHideHighlight(menuGameObject);

        // interactable.AddToHideHighlight(queueSlotsGameObject);
        // interactable.AddToHideHighlight(progressGameObject);
        // interactable.AddToHideHighlight(cancelButtonGameObject);
    }

    private void InitializeTitleDisplay()
    {
        titleGameObject = new GameObject("_title");
        titleGameObject.transform.position = new Vector3(0.0f, titleDisplayVerticalOffset, 0.0f);
        titleGameObject.transform.SetParent(interactionPanelObject.transform, false);

        TextMeshPro titleText = titleGameObject.AddComponent<TextMeshPro>();
        titleText.SetText(title);
        titleText.fontStyle = FontStyles.Bold;
        titleText.fontSize = 1.25f;
        titleText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        titleText.color = titleColor;
        titleText.raycastTarget = false;
    }

    private void InitializeHealthBarDisplay()
    {
        healthBarGameObject = new GameObject("_health_bar");
        healthBarGameObject.transform.position = new Vector3(0.0f, healthBarVerticalOffset, 0.0f);
        healthBarGameObject.transform.SetParent(interactionPanelObject.transform, false);

        if (showBarBackground)
        {
            GameObject healthBarBackroundGameObject = new GameObject("_health_bar_background");
            healthBarBackroundGameObject.transform.SetParent(healthBarGameObject.transform, false);
            healthBarBackroundGameObject.AddComponent<Canvas>().sortingOrder = 0;

            healthBarBackgroundImage = healthBarBackroundGameObject.AddComponent<Image>();
            healthBarBackroundGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
            healthBarBackgroundImage.sprite = GameMaster.Instance.healthBarBackground;
            healthBarBackgroundImage.color = GameMaster.Instance.healthBarBackgroundColor;
        }

        GameObject healthBarForegroundGameObject = new GameObject("_health_bar_foreground");
        healthBarForegroundGameObject.transform.SetParent(healthBarGameObject.transform, false);

        healthBarForegroundGameObject.AddComponent<Canvas>().sortingOrder = 1;

        healthBarForegroundImage = healthBarForegroundGameObject.AddComponent<Image>();
        healthBarForegroundGameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
        healthBarForegroundImage.sprite = GameMaster.Instance.healthBarForeground;
        healthBarForegroundImage.type = Image.Type.Filled;
        healthBarForegroundImage.fillMethod = Image.FillMethod.Horizontal;
        healthBarForegroundImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        healthBarForegroundImage.raycastTarget = false;
        healthBarForegroundImage.color = GameMaster.Instance.healthBarForegroundColor;

        if (showHealthText)
        {
            GameObject healthBarTextObject = new GameObject("_health_bar_text");
            healthBarTextObject.transform.SetParent(healthBarGameObject.transform, false);
            healthBarTextObject.AddComponent<Canvas>();

            healthBarText = healthBarTextObject.AddComponent<TextMeshPro>();
            //healthBarText.SetText(title);
            healthBarText.fontStyle = FontStyles.Bold;
            healthBarText.fontSize = 0.8f;
            healthBarText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            healthBarText.verticalAlignment = VerticalAlignmentOptions.Middle;
            healthBarText.raycastTarget = false;
            healthBarText.sortingOrder = 2;
            healthBarText.color = GameMaster.Instance.healthBarTextColor;
        }

        damageable = this.gameObject.GetComponent<Damageable>();

        if (damageable)
        {
            damageable.OnDamageEvent += OnDamage;
            damageable.OnHealthRegainEvent += OnHealthRegainEvent;
            SetHealthBarFilledAmount(damageable.Attributes.ValueOf(AttributeType.HEALTH));
        }
        else
            Debug.Log("Damageable component not found in parent.", this);
    }

    private Vector3 CalculateQueueButtonsStartPosition()
    {
        Vector3 startPosition = Vector3.zero;
        float buttonsTotalWidth = ((buttonSize + spaceBetweenButtons) * maxButtonColumns);

        // Don't count buttons that won't be displayed immediately.
        int count =  queueTechButtons.Count - queueTechButtons.FindAll(x => x is TechResearcher).Count;
        int totalRows = Mathf.CeilToInt((float)count / (float)maxButtonColumns);
        //float buttonsTotalHeight = ((buttonSize + spaceBetweenButtons) * totalRows);

        startPosition.x = buttonsTotalWidth * -0.5f + (buttonSize * 0.5f + spaceBetweenButtons * 0.5f);
        startPosition.y = (buttonSize + spaceBetweenButtons) * 0.5f;
        startPosition.y += (buttonSize + spaceBetweenButtons) * (totalRows - 1);

        return startPosition;
    }

    private void RepositionQueueButtons()
    {
        Vector3 startPosition = CalculateQueueButtonsStartPosition();
        Vector3 nextButtonPosition = startPosition;

        int currentButtonColumn = 0;
        int currentButtonRow = 0;

        if (queueUnitButtons == null)
            queueUnitButtons = new List<QueueUnitButton>();

        canQueueUnits = false;

        foreach (QueueUnitButton queueButton in queueUnitButtons)
        {
            if (!queueButton.gameObject.activeSelf)
                continue;

            queueButton.transform.localPosition = nextButtonPosition;
            
            currentButtonColumn++;
            if (currentButtonColumn >= maxButtonColumns)
            {
                currentButtonColumn = 0;
                currentButtonRow++;
            }
            nextButtonPosition.x = startPosition.x + ((buttonSize + spaceBetweenButtons) * currentButtonColumn);
            nextButtonPosition.y = startPosition.y - ((buttonSize + spaceBetweenButtons) * currentButtonRow);
        }        
    }

    private void InitializeQueueButtons()
    {
        if (!buttonsGameObject)
        {
            buttonsGameObject = new GameObject("_buttons");
            buttonsGameObject.transform.SetParent(menuGameObject.transform, false);
        }

        Vector3 startPosition = CalculateQueueButtonsStartPosition();
        titleGameObject.transform.localPosition = new Vector3(0.0f, startPosition.y + (buttonSize * 0.85f), 0.0f); 
        Vector3 nextButtonPosition = startPosition;

        int currentButtonColumn = 0;
        int currentButtonRow = 0;

        if (queueUnitButtons == null)
            queueUnitButtons = new List<QueueUnitButton>();

        canQueueUnits = false;

        foreach (TechBase tech in queueTechButtons)
        {
            if (tech is UnitData)
                canQueueUnits = true;            
            
            GameObject button = GenerateQueueButton(tech, nextButtonPosition, buttonsGameObject.transform);
            
            TechNode node = PlayerManager.Instance.currentTree.FindNode(tech);
            TechBase researcher = node.techRequirements.Find(x => x is TechResearcher);
            if (researcher)
            {
                button.gameObject.SetActive(false);
                //button.gameObject.transform.localPosition = queueUnitButtons.Find(x => x.techToQueue == researcher).transform.localPosition;
                continue;
            }
            
            currentButtonColumn++;
            if (currentButtonColumn >= maxButtonColumns)
            {
                currentButtonColumn = 0;
                currentButtonRow++;
            }
            nextButtonPosition.x = startPosition.x + ((buttonSize + spaceBetweenButtons) * currentButtonColumn);
            nextButtonPosition.y = startPosition.y - ((buttonSize + spaceBetweenButtons) * currentButtonRow);
        }
    }

    public void Toggle()
    {
        if (isVisible)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        if (disableSelf)
            this.enabled = true;

        isVisible = true;
        titleGameObject?.SetActive(true);
        menuGameObject?.SetActive(true);
        healthBarGameObject?.SetActive(true);

        if (canQueueUnits && unitRallyWaypoint)
            unitRallyWaypoint.gameObject.SetActive(true);

        foreach (GameObject go in objectsToAutohide)
            go.SetActive(true);
    }

    public void Hide()
    {
        titleGameObject?.SetActive(false);
        menuGameObject?.SetActive(false);

        if (unitRallyWaypoint && canQueueUnits)
            unitRallyWaypoint.gameObject.SetActive(false);

        // TODO: Should be based on the healthbars autoshowAt/autohideAt values.
        // TODO: Change to healthbar events that autohideBillboard can subscribe to.
        // if (healthBar.isVisible)
        //     autoHideBillboard.enabled = true;
        // else
        //     autoHideBillboard.enabled = false;

        if (healthBarForegroundImage.fillAmount < healthBarAutoShowAt &&
            healthBarForegroundImage.fillAmount > 0)
            return;

        healthBarGameObject.SetActive(false);

        foreach (GameObject go in objectsToAutohide)
            go.SetActive(false);

        if (disableSelf)
            this.enabled = false;

        isVisible = false;
    }

    private void SetHealthBarFilledAmount(float amount)
    {
        if (GameMaster.Instance.healthBarForeground)
            healthBarForegroundImage.fillAmount = amount;

        if (showHealthText)
            healthBarText.text = (damageable.GetHealthPercent() * 100.0f).ToString() + "%";

        if (amount >= healthBarAutoHideAt)
            healthBarGameObject.SetActive(false);
        else if (amount < healthBarAutoShowAt)
            healthBarGameObject.SetActive(true);

        if (amount <= 0.0f)
            healthBarGameObject.SetActive(false); ;
    }


    private GameObject GenerateQueueButton(TechBase tech, Vector3 position, Transform parent)
    {
        //---------------------------------------------------------------------
        // Button
        GameObject button = new GameObject("_button", typeof(QueueUnitButton));
        button.transform.SetParent(parent, false);
        button.transform.localPosition = position;
        button.name = string.Format("_queue_{0}_button", tech.title.ToString());
        button.transform.localScale = new Vector3(buttonSize, buttonSize, buttonSize);

        Interactable buttonInteractable = button.AddComponent<Interactable>();
        buttonInteractable.highlightOnHover = false;
        buttonInteractable.highlightOnPointedAt = false;
        buttonInteractable.highlightMaterial = GameMaster.Instance.interactionHighlightMaterial;

        QueueUnitButton queueUnitButton = button.GetComponent<QueueUnitButton>();
        queueUnitButton.techToQueue = tech;
        queueUnitButtons.Add(queueUnitButton);

        //---------------------------------------------------------------------
        // Base (child of Button)
        GameObject buttonBase = Instantiate(GameMaster.Instance.buttonBasePrefab, Vector3.zero, Quaternion.identity, button.transform);
        buttonBase.name = "_base";
        buttonBase.transform.localRotation = Quaternion.identity;
        buttonBase.transform.localPosition = Vector3.zero;
        buttonBase.transform.GetComponent<MeshRenderer>().sharedMaterial = GameMaster.Instance.buttonBaseMaterial;

        //---------------------------------------------------------------------
        // Instantiate the resource cost gameobject
        GameObject resourceCost = Instantiate(GameMaster.Instance.interactionPanelResourceCostPrefab, Vector3.zero, Quaternion.identity, button.transform);
        resourceCost.transform.localPosition = new Vector3(0.0f, -0.462f, -0.092f);
        resourceCost.transform.localRotation = Quaternion.identity;

        //---------------------------------------------------------------------
        // Popluate the resource cost prefab text objects
        BuildMenuResouceCost cost = resourceCost.GetComponent<BuildMenuResouceCost>();
        cost.woodText.text = tech.woodCost.ToString();
        cost.goldText.text = tech.goldCost.ToString();
        cost.grainText.text = tech.foodCost.ToString();
        cost.stoneText.text = tech.stoneCost.ToString();

        //---------------------------------------------------------------------
        // Button label
        GameObject textObject = new GameObject("_button_label");
        TMPro.TextMeshPro text = textObject.AddComponent<TMPro.TextMeshPro>();
        textObject.transform.SetParent(button.transform, false);
        text.rectTransform.sizeDelta = new Vector2(1.0f, 0.25f);
        text.rectTransform.localPosition = new Vector3(0.0f, 0.484f, -0.088f);
        text.sortingOrder = 1;
        text.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
        text.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        text.fontSize = 1.0f;
        text.font = GameMaster.Instance.progressFont;
        text.text = tech.title;

        if (tech is TechResearcher)
            text.color = Color.yellow;

        //---------------------------------------------------------------------
        // Face (child of Button)
        GameObject face = new GameObject("_face", typeof(Interactable), typeof(HoverButton));
        face.transform.SetParent(button.transform, false);
        face.transform.localPosition = new Vector3(0.0f, 0.0f, -0.05f);

        Interactable faceInteractable = face.GetComponent<Interactable>();
        faceInteractable.highlightOnHover = false;
        faceInteractable.highlightOnPointedAt = false;
        faceInteractable.highlightMaterial = GameMaster.Instance.interactionHighlightMaterial;

        //---------------------------------------------------------------------
        // Hover button
        HoverButton hoverButton = face.GetComponent<HoverButton>();
        hoverButton.localMoveDistance = new Vector3(0, 0, 0.04f);
        hoverButton.onButtonDown.AddListener(OnQueueHoverButtonDown);
        hoverButton.onButtonUp.AddListener(OnQueueHoverButtonUp);

        //---------------------------------------------------------------------
        // Set up and initialize queue
        if (unitSpawnPoint)
            techQueue.SetUnitSpawnPointTransform(unitSpawnPoint);

        if (unitRallyWaypoint)
            techQueue.SetUnitRallyPointTransform(unitRallyWaypoint);

        techQueue.Initialize();

        //---------------------------------------------------------------------
        // MovingPart (child of Face)
        GameObject buttonMovingPart = Instantiate(GameMaster.Instance.buttonMovingPartPrefab, Vector3.zero, Quaternion.identity, face.transform);
        buttonMovingPart.name = "_moving_part";
        buttonMovingPart.transform.localPosition = Vector3.zero;
        buttonMovingPart.transform.localRotation = Quaternion.identity;
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        mat.SetTexture("_MainTex", tech.worldButtonTexture);
        buttonMovingPart.transform.GetComponent<MeshRenderer>().sharedMaterial = mat;

        hoverButton.movingPart = buttonMovingPart.transform;
        button.transform.localRotation = Quaternion.identity;

        //---------------------------------------------------------------------
        // Lock (child of Button)

        GameObject buttonLock = Instantiate<GameObject>(PlayerManager.Instance.GetButtonLockPrefab(tech));
        buttonLock.name = "_lock";
        buttonLock.transform.SetParent(button.transform, false);
        buttonLock.transform.localPosition = new Vector3(0.0f, 0.0f, -0.14f);
        buttonLock.transform.Rotate(0.0f, 180.0f, 0.0f);
        buttonLock.SetActive(false);

        queueUnitButton.buttonLockedObject = buttonLock;
        queueUnitButton.Initialize();
        if (PlayerManager.Instance.faction.techTree.IsUnlocked(tech))
            queueUnitButton.Unlock();
        else
            queueUnitButton.Lock();

        Destroy(buttonBase.GetComponent<BoxCollider>());

        return button;
    }

    public void OnQueueHoverButtonDown(Hand hand)
    {
        // TODO: Change this so Hand has references to it's interaction pointer and
        // pointer interactables or create InteractionPointer events
        QueueUnitButton queueUnitButton = InteractionPointer.Instance.PointedAtQueueButton;
        if (queueUnitButton)
            techQueue.QueueTech(queueUnitButton.techToQueue);

        PlayerManager.Instance.PlayQueueButtonDownSound();
    }

    public void OnQueueHoverButtonUp(Hand hand)
    {
        PlayerManager.Instance.PlayQueueButtonUpSound();
    }

    public void OnCancelQueueHoverButtonDown(Hand hand)
    {
        PlayerManager.Instance.PlayQueueButtonDownSound();
        techQueue.DequeueUnit();
    }

    private void InitializeQueueSlots(Transform parent, int maxColumns, float padding)
    {
        queueSlotImages = new Image[numberOfQueueSlots];

        int currentQueueSlotRow = 0;
        int currentQueueSlotColumn = 0;

        float buttonsTotalWidth = ((buttonSize + spaceBetweenButtons) * maxButtonColumns);
        queueSlotSize = (buttonsTotalWidth - padding) / maxColumns - padding;

        Vector3 startPosition = Vector3.zero;
        startPosition.x = buttonsTotalWidth * -0.5f + (queueSlotSize * 0.5f + padding);// * 0.5f);
        startPosition.y = (queueSlotSize + padding) * -0.5f;

        cancelButtonPosition = startPosition;
        cancelButtonPosition.x -= buttonSize + spaceBetweenQueueSlots;
        cancelButtonPosition.y -= queueSlotSize * 0.5f;

        Vector3 nextButtonPosition = startPosition;
        techQueue.SetSpawnQueueSlotCount(numberOfQueueSlots);

        for (int i = numberOfQueueSlots - 1; i >= 0; i--)
        {
            queueSlotImages[i] = GenerateQueueSlot(nextButtonPosition, parent);
            techQueue.SetQueueSlotImage(queueSlotImages[i], (byte)i);

            currentQueueSlotColumn++;
            if (currentQueueSlotColumn >= maxColumns)
            {
                currentQueueSlotColumn = 0;
                currentQueueSlotRow++;
            }

            nextButtonPosition.x = startPosition.x + ((queueSlotSize + padding) * currentQueueSlotColumn);
            nextButtonPosition.y = startPosition.y - ((queueSlotSize + padding) * currentQueueSlotRow);
        }

        progressPosition.x = nextButtonPosition.x + (spaceBetweenQueueSlots + queueSlotSize) * 0.5f;
        progressPosition.y = nextButtonPosition.y;

        healthBarGameObject.transform.localPosition = new Vector3(0.0f, nextButtonPosition.y - 0.2f, 0.0f);
    }

    private Image GenerateQueueSlot(Vector3 position, Transform parent)
    {
        GameObject imageObject = new GameObject("_queue_slot_image");
        imageObject.AddComponent<Canvas>();
        Image image = imageObject.AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.rectTransform.sizeDelta = new Vector2(queueSlotSize, queueSlotSize);
        image.rectTransform.anchoredPosition = position;
        image.sprite = Sprite.Create(GameMaster.Instance.emptyQueueSlotTexture, new Rect(0f, 0f, GameMaster.Instance.emptyQueueSlotTexture.width, GameMaster.Instance.emptyQueueSlotTexture.height), new Vector2(0.5f, 0.5f), 100.0f, 1, SpriteMeshType.Tight, Vector4.zero, true);

        return image;
    }

    private void InitializeProgressBar(Vector3 position, Vector2 size, float verticalPadding, Transform parent)
    {
        GameObject imageObject = new GameObject("_progress_image");
        imageObject.AddComponent<Canvas>();
        Image image = imageObject.AddComponent<Image>();
        image.transform.SetParent(parent, false);
        Vector2 paddedSize = size;
        paddedSize.y -= verticalPadding;
        image.rectTransform.sizeDelta = paddedSize;
        image.rectTransform.anchoredPosition = position;
        image.sprite = GameMaster.Instance.progressImage;
        image.color = GameMaster.Instance.progressColor;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;

        GameObject textObject = new GameObject("_progress_text");
        TMPro.TextMeshPro text = textObject.AddComponent<TMPro.TextMeshPro>();
        textObject.transform.SetParent(parent, false);
        text.rectTransform.sizeDelta = size;
        text.rectTransform.anchoredPosition = position;
        text.sortingOrder = 1;
        text.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
        text.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        text.fontSize = 1.0f;
        text.font = GameMaster.Instance.progressFont;
        text.text = "100%";

        if (enableQueueMenu)
        {
            techQueue.SetProgressText(text);
            techQueue.SetProgressImage(image);
        }
    }

    private void InitializeCancelButton(Transform parent)
    {
        // Base (child of parent)
        GameObject buttonBase = Instantiate(GameMaster.Instance.buttonBasePrefab, Vector3.zero, Quaternion.identity, parent);
        buttonBase.name = "_base";
        buttonBase.transform.localRotation = Quaternion.identity;
        buttonBase.transform.localPosition = Vector3.zero;
        buttonBase.transform.GetComponent<MeshRenderer>().sharedMaterial = GameMaster.Instance.buttonBaseMaterial;

        // Face (child of parent)
        GameObject face = new GameObject("_face", typeof(Interactable), typeof(HoverButton));
        face.transform.SetParent(parent, false);
        face.transform.localPosition = new Vector3(0.0f, 0.0f, -0.05f);

        Interactable interactable = face.GetComponent<Interactable>();
        interactable.highlightOnHover = false;
        interactable.highlightOnPointedAt = false;
        interactable.highlightMaterial = GameMaster.Instance.interactionHighlightMaterial;

        HoverButton hoverButton = face.GetComponent<HoverButton>();
        hoverButton.localMoveDistance = new Vector3(0, 0, 0.04f);
        hoverButton.onButtonDown.AddListener(OnCancelQueueHoverButtonDown);

        // MovingPart (child of Face)
        GameObject buttonMovingPart = Instantiate(GameMaster.Instance.buttonMovingPartPrefab, Vector3.zero, Quaternion.identity, face.transform);
        buttonMovingPart.name = "_moving_part";
        buttonMovingPart.transform.localPosition = Vector3.zero;
        buttonMovingPart.transform.localRotation = Quaternion.identity;
        buttonMovingPart.transform.GetComponent<MeshRenderer>().sharedMaterial = GameMaster.Instance.cancelButtonMaterial;

        hoverButton.movingPart = buttonMovingPart.transform;

        Destroy(buttonBase.GetComponent<BoxCollider>());
    }

    public void OnHealthRegainEvent(object sender, Damageable.HealthRegainEvent e)
    {
        SetHealthBarFilledAmount(e.health / damageable.GetMaxHealth());
    }

    public void OnDamage(object sender, Damageable.DamageEvent e)
    {
        SetHealthBarFilledAmount((damageable.GetHealth() - e.damage) / damageable.GetMaxHealth());

        if (healthBarBackgroundImage.fillAmount <= 0.0f)
            healthBarGameObject.SetActive(false);
    }



    public delegate void OnAutohide();
    public event OnAutohide onAutoHide;
}
