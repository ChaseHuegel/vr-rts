using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Valve.VR.InteractionSystem;
using Swordfish;

[RequireComponent(typeof(PointerInteractable), typeof(Collider))]
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
    public List<TechBase> queueTechButtons;
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
    private SpawnQueue spawnQueue;
    private Damageable damageable;
    private GameObject cancelButtonGameObject;
    private bool isVisible;

    private List<QueueUnitButton> queueUnitButtons;
    private BuildingData buildingData;

    void Awake()
    {
        if (!faceTarget) faceTarget = Player.instance.hmdTransform;        
    }

    // Start is called before the first frame update
    void Start()
    {
        buildingData = GetComponent<Structure>().buildingData;
        if (buildingData.techQueueButtons.Count > 0)
        {
            enableQueueMenu = true;
            queueTechButtons = buildingData.techQueueButtons;
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
            spawnQueue = this.gameObject.AddComponent<SpawnQueue>();
            if (!spawnQueue)
                Debug.LogWarning("Missing SpawnQueue component.", this);

            InitializeMenuDisplay();

            InitializeSpawnQueue();

            this.gameObject.GetComponent<PointerInteractable>().AddChildrenToHideHighlight(interactionPanelObject);            

            HookIntoEvents();            
        }

        // TODO: Probably should be handled differently
        PlayerManager.Instance.faction.techTree.RefreshNodes();

        if (startHidden)
            Hide();
        else
            Show();        
    }

    // private void OnNodeUnlocked(TechNode node)
    // {
    //     queueUnitButtons.Find(x => x.techToQueue == node.tech)?.Unlock();
    // }

    // private void OnNodeLocked(TechNode node)
    // {
    //     queueUnitButtons.Find(x => x.techToQueue == node.tech)?.Lock();
    // }
    private void OnNodeResearched(TechNode node) 
    {
        // Do not destroy buttons if node is a unit node
        if (node.tech is UnitData)
            return;

        queueTechButtons.Remove(node.tech);

        // TODO: Just move the buttons into new positions rather
        // than regenerating them
        foreach(QueueUnitButton queueUnitButton in queueUnitButtons.ToArray())
        {
            GameObject obj = queueUnitButton.gameObject;
            Destroy(obj);
        }

        queueUnitButtons.Clear();
        InitializeQueueButtons();
        InitializeSpawnQueue();
    }

    // private void OnNodeEnabled(TechNode node) 
    // {
    //     queueUnitButtons.Find(x => x.techToQueue == node.tech)?.Enable();
    // }

    // private void OnNodeDisabled(TechNode node)
    // {
    //     queueUnitButtons.Find(x => x.techToQueue == node.tech)?.Disable();
    // }

    private void HookIntoEvents()
    {
        // TechTree.OnNodeUnlocked += OnNodeUnlocked;
        // TechTree.OnNodeLocked += OnNodeLocked;
        // TechTree.OnNodeEnabled += OnNodeEnabled;
        // TechTree.OnNodeDisabled += OnNodeDisabled;
        TechTree.OnNodeResearched += OnNodeResearched;

    }

    private void CleanupEvents()
    {
        // TechTree.OnNodeUnlocked -= OnNodeUnlocked;
        // TechTree.OnNodeLocked -= OnNodeUnlocked;
        // TechTree.OnNodeEnabled -= OnNodeEnabled;
        // TechTree.OnNodeDisabled -= OnNodeDisabled;
        TechTree.OnNodeResearched -= OnNodeResearched;
        
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
    }

    private void InitializeSpawnQueue()
    {
        // TODO: Should this stuff be moved to this class?
        spawnQueue.SetButtonsParentObject(buttonsGameObject);
        spawnQueue.SetButtonDownAudio(GameMaster.Instance.onButtonDownAudio);
        spawnQueue.SetButtonUpAudio(GameMaster.Instance.onButtonUpAudio);
        spawnQueue.SetCancelButton(cancelButtonGameObject.GetComponentInChildren<HoverButton>(true));

        if (unitSpawnPoint)
            spawnQueue.SetUnitSpawnPointTransform(unitSpawnPoint);

        if (unitRallyWaypoint)
            spawnQueue.SetUnitRallyPointTransform(unitRallyWaypoint);

        spawnQueue.Initialize();
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
            SetHealthBarFilledAmount(damageable.Attributes.ValueOf(AttributeConstants.HEALTH));
        }
        else
            Debug.Log("Damageable component not found in parent.", this);
    }

    private void InitializeMenuDisplay()
    {
        menuGameObject = new GameObject("_menu");
        menuGameObject.transform.position = new Vector3(0.0f, QueueMenuVerticalOffset, 0.0f);
        menuGameObject.transform.SetParent(interactionPanelObject.transform, false);
        
        InitializeQueueButtons();

        GameObject queueSlotsGameObject = new GameObject("_queue_slots");
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
        cancelButtonGameObject.AddComponent<PointerInteractable>();
        InitializeCancelButton(cancelButtonGameObject.transform);
    }

    private void InitializeQueueButtons()
    {
        if (!buttonsGameObject)
        {
            buttonsGameObject = new GameObject("_buttons");
            buttonsGameObject.transform.SetParent(menuGameObject.transform, false);
        }        

        Vector3 startPosition = Vector3.zero;
        float buttonsTotalWidth = ((buttonSize + spaceBetweenButtons) * maxButtonColumns);
        int totalRows = Mathf.CeilToInt((float)queueTechButtons.Count / (float)maxButtonColumns);
        //float buttonsTotalHeight = ((buttonSize + spaceBetweenButtons) * totalRows);

        startPosition.x = buttonsTotalWidth * -0.5f + (buttonSize * 0.5f + spaceBetweenButtons * 0.5f);
        startPosition.y = (buttonSize + spaceBetweenButtons) * 0.5f;
        startPosition.y += (buttonSize + spaceBetweenButtons) * (totalRows - 1);

        titleGameObject.transform.localPosition = new Vector3 (0.0f, startPosition.y + (buttonSize * 0.85f), 0.0f);
        Vector3 nextButtonPosition = startPosition;

        int currentButtonColumn = 0;
        int currentButtonRow = 0;

        if (queueUnitButtons == null)
            queueUnitButtons = new List<QueueUnitButton>();

        
        foreach (TechBase tech in queueTechButtons)
        {
            GenerateQueueButton(tech, nextButtonPosition, buttonsGameObject.transform);

            currentButtonColumn++;
            if (currentButtonColumn >= maxButtonColumns)
            {
                currentButtonColumn = 0;
                currentButtonRow++;
            }
            nextButtonPosition.x = startPosition.x + ((buttonSize + spaceBetweenButtons) * currentButtonColumn);
            nextButtonPosition.y = startPosition.y - ((buttonSize + spaceBetweenButtons) * currentButtonRow);
        }

        foreach (QueueUnitButton btn in queueUnitButtons)
            btn.Initialize();
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
        unitRallyWaypoint?.gameObject.SetActive(true);

        foreach (GameObject go in objectsToAutohide)
            go.SetActive(true);
    }

    public void Hide()
    {        
        titleGameObject?.SetActive(false);
        menuGameObject?.SetActive(false);

        if (enableQueueMenu && unitRallyWaypoint)
            unitRallyWaypoint.gameObject?.SetActive(false);

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
            healthBarText.text = (damageable.GetHealthPercent() * 100.0f ).ToString() + "%";

        if (amount >= healthBarAutoHideAt)
            healthBarGameObject.SetActive(false);
        else if (amount < healthBarAutoShowAt)
            healthBarGameObject.SetActive(true);

        if (amount <= 0.0f)
            healthBarGameObject.SetActive(false);;
    }

    

    private GameObject GenerateQueueButton(TechBase tech, Vector3 position, Transform parent)
    {
        //UnitData typeData = GameMaster.GetUnit(unitType);

        // Button
        GameObject button = new GameObject("_button", typeof(QueueUnitButton));
        button.transform.SetParent(parent, false);
        button.transform.localPosition = position;
        button.name = string.Format("_queue_{0}_button", tech.title.ToString());
        button.transform.localScale = new Vector3(buttonSize, buttonSize, buttonSize);
        button.AddComponent<PointerInteractable>();

        QueueUnitButton queueUnitButton = button.GetComponent<QueueUnitButton>();
        queueUnitButton.techToQueue = tech;
        queueUnitButtons.Add(queueUnitButton);        

        // Base (child of Button)
        GameObject buttonBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonBase.name = "_base";
        buttonBase.transform.SetParent(button.transform, false);
        buttonBase.transform.localScale = new Vector3(1.0f, 1.0f, 0.15f);
        buttonBase.transform.localPosition = Vector3.zero;
        buttonBase.transform.GetComponent<MeshRenderer>().sharedMaterial = GameMaster.Instance.buttonBaseMaterial;

        // Instantiate the resource cost gameobject
        // GameObject resourceCost = Instantiate(resourceCostPrefab, Vector3.zero, Quaternion.identity, button.transform);
        // resourceCost.transform.localPosition = new Vector3(0.0f, -0.462f, 0.008f);
        // resourceCost.transform.localRotation = Quaternion.identity;

        // Popluate the resource cost prefab text objects
        // BuildMenuResouceCost cost = resourceCost.GetComponent<BuildMenuResouceCost>();
        // cost.woodText.text = typeData.woodCost.ToString();
        // cost.goldText.text = typeData.goldCost.ToString();
        // cost.grainText.text = typeData.foodCost.ToString();
        // cost.stoneText.text = typeData.stoneCost.ToString();

        // Face (child of Button)
        GameObject face = new GameObject("_face", typeof(Interactable), typeof(HoverButton));
        face.transform.SetParent(button.transform, false);
        face.transform.localPosition = new Vector3(0.0f, 0.0f, -0.05f);
        face.transform.localScale = new Vector3(0.85f, 0.85f, 0.15f);
        
        HoverButton hoverButton = face.GetComponent<HoverButton>();
        hoverButton.localMoveDistance = new Vector3(0, 0, 0.3f);
        face.GetComponent<Interactable>().highlightOnHover = false;        

        // MovingPart (child of Face)
        GameObject buttonMovingPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonMovingPart.AddComponent<UVCubeMap>();
        buttonMovingPart.name = "_moving_part";
        buttonMovingPart.transform.SetParent(face.transform, false);
        buttonMovingPart.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        buttonMovingPart.transform.localPosition = Vector3.zero;
        buttonMovingPart.transform.GetComponent<MeshRenderer>().sharedMaterial = tech.worldButtonMaterial;

        hoverButton.movingPart = buttonMovingPart.transform;
        button.transform.localRotation = Quaternion.identity;

        // Lock (child of Button)
        if (GameMaster.Instance.buttonLockPrefab)
        {
            GameObject buttonLock = Instantiate<GameObject>(GameMaster.Instance.buttonLockPrefab);
            buttonLock.name = "_lock";
            buttonLock.transform.SetParent(button.transform, false);
            buttonLock.transform.localPosition = new Vector3(0.0f, 0.0f, -0.14f);
            buttonLock.transform.Rotate(0.0f, 180.0f, 0.0f);

            queueUnitButton.buttonLockedObject = buttonLock;
            //queueUnitButton.SetLocked(!PlayerManager.Instance.faction.techTree.IsUnlocked(tech));
        }

        Destroy(buttonBase.GetComponent<BoxCollider>());

        return button;
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
        spawnQueue.SetSpawnQueueSlotCount(numberOfQueueSlots);

        for (int i = numberOfQueueSlots - 1; i >= 0; i--)
        {
            queueSlotImages[i] = GenerateQueueSlot(nextButtonPosition, parent);
            spawnQueue.SetQueueSlotImage(queueSlotImages[i], (byte)i);

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
        image.sprite = GameMaster.Instance.emptyQueueSlotSprite;

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
            spawnQueue.SetProgressText(text);
            spawnQueue.SetProgressImage(image);
        }
    }

    private void InitializeCancelButton(Transform parent)
    {
        // Base (child of parent)
        GameObject buttonBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonBase.name = "_base";
        buttonBase.transform.SetParent(parent, false);
        buttonBase.transform.localScale = new Vector3(1.0f, 1.0f, 0.15f);
        buttonBase.transform.localPosition = Vector3.zero;
        buttonBase.transform.GetComponent<MeshRenderer>().sharedMaterial = GameMaster.Instance.buttonBaseMaterial;        

        // Face (child of parent)
        GameObject face = new GameObject("_face", typeof(Interactable), typeof(HoverButton));
        face.transform.SetParent(parent, false);
        face.transform.localPosition = new Vector3(0.0f, 0.0f, -0.05f);
        face.transform.localScale = new Vector3(0.85f, 0.85f, 0.15f);
        HoverButton hoverButton = face.GetComponent<HoverButton>();
        hoverButton.localMoveDistance = new Vector3(0, 0, 0.3f);
        face.GetComponent<Interactable>().highlightOnHover = false;


        // MovingPart (child of Face)
        GameObject buttonMovingPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonMovingPart.AddComponent<UVCubeMap>();
        buttonMovingPart.name = "_moving_part";
        buttonMovingPart.transform.SetParent(face.transform, false);
        buttonMovingPart.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        buttonMovingPart.transform.localPosition = Vector3.zero;
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
