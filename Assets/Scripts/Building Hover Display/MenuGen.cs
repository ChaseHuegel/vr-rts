using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish;
using Swordfish.Audio;
using UnityEditor;
using Valve.VR.InteractionSystem;
using Swordfish.Navigation;
using UnityEngine.UI;

public class MenuGen : MonoBehaviour
{
    [Header("Menu Generation Settings")]
    public Vector3 position = new Vector3(0.0f, 0.0f, 10.0f);
    public Vector3 rotation = new Vector3(0.0f, 90.0f, 90.0f);
        

    [Header("Button Settings")]    
    public float buttonSize = 2.0f;
    public float spaceBetweenButtons = 0.15f;
    public int buttonColumns = 5;
    public Material buttonBaseMaterial;
    public Material cancelButtonMaterial;
    public GameObject buttonLockPrefab;
    public AudioClip onButtonDownAudio;
    public AudioClip onButtonUpAudio;
    private Vector3 buttonsGenOffset = Vector3.zero;

    [Header("Progress Bar Settings")]
    public Sprite progressSprite;    
    public Color progressSpriteColor;
    public TMPro.TMP_FontAsset progressFont;
    private TMPro.TMP_Text progressText;
    private UnityEngine.UI.Image progressImage;

    [Header("Queue Buttons")]
        public GameObject resourceCostPrefab;
    public List<RTSUnitType> queueUnitButtons;


    [Header("Queue Slot Settings")]
    public byte numberOfQueueSlots = 12;
    public Sprite emptyQueueSlotSprite;
    private float queueSlotSize = 1.0f;
    private float spaceBetweenQueueSlots = 0.15f;
    private UnityEngine.UI.Image[] queueSlotImages;

    // [Header("Unit Spawn Queue Settings")]
    // public Transform unitSpawnPoint;
    // public Transform unitRallyWaypoint;

    // Meant to be used so units pick a random spot within the radius to
    // go to so they don't fight over a single point.
    // public float unitRallyWaypointRadius;

    // protected float timeElapsed = 0.0f;
    // protected LinkedList<UnitData> unitSpawnQueue = new LinkedList<UnitData>();
    // private Structure structure;
    // private Damageable damageable;
    // protected AudioSource audioSource;
    // private RTSUnitType lastUnitQueued;
    // private PlayerManager playerManager;
    private Vector3 nextButtonPosition;

    // Holds the next position of the next queue slot, position used as
    // start position of queue progress bar.
    private Vector3 progressPosition;
    private Vector3 cancelButtonPosition;
    private GameObject menuParentObject;
    private Transform buttonsParent;
    private HoverButton cancelButton;
    private int currentButtonRow = 0;
    private int currentButtonColumn = 0;

    private SpawnQueue spawnQueue;

    private bool IsCreatingUnitQueueMenu;

    void Start()
    {
        if (!(spawnQueue = gameObject.GetComponentInParent<SpawnQueue>()))
            Debug.Log("Missing SpawnQueue component in parent.", this);
    }

    // [ExecuteInEditMode]
    // public void GenerateButtonMenu(bool autoselect)
    // {
    //     IsCreatingUnitQueueMenu = false;
    //     GenerateMenu(autoselect);
    // }

    [ExecuteInEditMode]
    public void GenerateQueueMenu(bool autoselect)
    {
        IsCreatingUnitQueueMenu = true;
        GenerateMenu(autoselect);
    }

    [ExecuteInEditMode]
    public void GenerateMenu(bool autoselect)
    {
        // Create a new child to hold the menu, never delete child objects...
        // leave deletion to the user.
        Transform menuParent = CreateMenuParent(this.transform);

        if (IsCreatingUnitQueueMenu)
        {
            if (!spawnQueue)
                if (!(spawnQueue = gameObject.GetComponentInParent<SpawnQueue>()))
                    Debug.Log("Missing SpawnQueue component in parent.", this);

            spawnQueue.SetMenuParentObject(menuParent.gameObject);
            spawnQueue.SetSpawnQueueSlotCount(numberOfQueueSlots);
            spawnQueue.SetButtonDownAudio(onButtonDownAudio);
            spawnQueue.SetButtonUpAudio(onButtonUpAudio);

            GameObject queueSlotsParent = new GameObject("_QueueSlots");
            queueSlotsParent.AddComponent<Canvas>();
            queueSlotsParent.transform.SetParent(menuParent, false);
            queueSlotsParent.transform.localPosition = Vector3.zero;
            GenerateQueueSlots(queueSlotsParent.transform, 7, 0.10f);

            GameObject cancelButton = new GameObject("_CancelButton");
            cancelButton.transform.SetParent(menuParent, false);
            cancelButton.transform.localPosition = cancelButtonPosition;
            GenerateCancelButton(cancelButton.transform);

            GameObject progress = new GameObject("_Progress");
            progress.AddComponent<Canvas>();
            progress.transform.SetParent(menuParent, false);
            Vector2 size = new Vector2(queueSlotSize * 2.0f + spaceBetweenQueueSlots, queueSlotSize);
            GenerateProgressBar(progressPosition, size, 0.30f, progress.transform);

            buttonsParent = CreateButtonsParent(menuParent);
            spawnQueue.SetButtonsParentObject(buttonsParent.gameObject);
            GenerateButtons(ResetGeneration(), buttonsParent);
        }

        else if (!IsCreatingUnitQueueMenu)
        {
            GameObject cancelButton = new GameObject("_CancelButton");
            cancelButton.transform.SetParent(menuParent, false);
            float halfButtonOffset = (buttonSize + spaceBetweenButtons);// * 0.5f;
            ResetGeneration();
            buttonsGenOffset.y = (buttonSize + spaceBetweenButtons) * 0.5f;
            nextButtonPosition = buttonsGenOffset;
            cancelButtonPosition = new Vector3(buttonsGenOffset.x + halfButtonOffset,
                                                buttonsGenOffset.y);
            cancelButton.transform.localPosition = cancelButtonPosition;
            GenerateCancelButton(cancelButton.transform);

            GameObject progress = new GameObject("_Progress");
            progress.AddComponent<Canvas>();
            progress.transform.SetParent(menuParent, false);

            float buttonsTotalWidth = ((buttonSize + spaceBetweenButtons) * buttonColumns);
            Vector2 size = new Vector2(buttonsTotalWidth, queueSlotSize);
            progressPosition.x = 0;
            GenerateProgressBar(progressPosition, size, 0, progress.transform);

            buttonsParent = CreateButtonsParent(menuParent);
            GenerateButtons(nextButtonPosition, buttonsParent);
        }

        if (autoselect)
        {
            UnityEditor.Selection.activeGameObject = null;
            UnityEditor.Selection.activeGameObject = menuParent.gameObject;
        }
    }
    
    [ExecuteInEditMode]
    private Transform CreateMenuParent(Transform parent)
    {
        menuParentObject = new GameObject("_MenuParent");
        menuParentObject.transform.SetParent(parent, false);
        menuParentObject.transform.localPosition = position;
        menuParentObject.transform.Rotate(rotation);
        nextButtonPosition = ResetGeneration();
        return menuParentObject.transform;
    }

    [ExecuteInEditMode]
    private Transform CreateButtonsParent(Transform parent)
    {
        GameObject buttonsParent = new GameObject("_Buttons");
        buttonsParent.transform.SetParent(parent, false);
        buttonsParent.transform.localPosition = Vector3.zero;
        return buttonsParent.transform;
    }

    [ExecuteInEditMode]
    private Vector3 ResetGeneration()
    {
        Vector3 buttonsOffset = Vector3.zero;
        float buttonsTotalWidth = ((buttonSize + spaceBetweenButtons) * buttonColumns);
        int totalRows = Mathf.CeilToInt((float)queueUnitButtons.Count / (float)buttonColumns);
        float buttonsTotalHeight = ((buttonSize + spaceBetweenButtons) * totalRows);

        // Half button offset htx
        buttonsOffset.x = (buttonSize + spaceBetweenButtons) * -0.5f;
        buttonsOffset.x += (buttonsTotalWidth * 0.5f);

        buttonsOffset.y = (buttonSize + spaceBetweenButtons) * 0.5f;
        buttonsOffset.y += (buttonSize + spaceBetweenButtons) * (totalRows - 1);

        buttonsGenOffset = buttonsOffset;
        
        currentButtonRow = 0;
        currentButtonColumn = 0;
        
        return buttonsOffset;
    }

    [ExecuteInEditMode]
    private void GenerateButtons(Vector3 startPosition, Transform parent)
    {
        nextButtonPosition = startPosition;

        foreach (RTSUnitType unitType in queueUnitButtons)
        {
            if (IsCreatingUnitQueueMenu)
                AddQueueButton(unitType);
        }
    }  

    [ExecuteInEditMode]
    private void GenerateQueueSlots(Transform parent, int maxColumns, float padding)
    {
        queueSlotImages = new Image[numberOfQueueSlots];
        
        int currentQueueSlotRow = 0;
        int currentQueueSlotColumn = 0;

        float menuTotalWidth = ((buttonSize + spaceBetweenButtons) * buttonColumns);
        queueSlotSize = (menuTotalWidth - padding) / maxColumns - spaceBetweenQueueSlots;
        
        //queueSlotSize = 0.8214286f; // Original size

        Vector3 slotOffset = Vector3.zero;
        slotOffset.x = (menuTotalWidth * 0.5f) - (padding * 0.5f);
        slotOffset.x += (queueSlotSize + spaceBetweenQueueSlots) * -0.5f;

        slotOffset.y = (queueSlotSize + spaceBetweenQueueSlots) * -0.5f;

        cancelButtonPosition = slotOffset;
        cancelButtonPosition.x += queueSlotSize + spaceBetweenQueueSlots;

        Vector3 slotPosition = slotOffset;
        
        for (int i = numberOfQueueSlots - 1; i >= 0; i--)
        {                       
            queueSlotImages[i] = GenerateQueueSlot(slotPosition, parent);
            spawnQueue.SetQueueSlotImage(queueSlotImages[i], (byte)i);

            currentQueueSlotColumn++;
            if (currentQueueSlotColumn >= maxColumns)
            {
                currentQueueSlotColumn = 0;
                currentQueueSlotRow++;
            }

            slotPosition.x = slotOffset.x - ((queueSlotSize + spaceBetweenQueueSlots) * currentQueueSlotColumn);
            slotPosition.y = slotOffset.y - ((queueSlotSize + spaceBetweenQueueSlots) * currentQueueSlotRow);
        }

        progressPosition.x = slotPosition.x - (spaceBetweenQueueSlots + queueSlotSize) * 0.5f;
        progressPosition.y = slotPosition.y;
    }

    [ExecuteInEditMode]
    private void GenerateProgressBar(Vector3 position, Vector2 size, float verticalPadding, Transform parent)
    {
        GameObject imageObject = new GameObject("_progressImage");
        Image image = imageObject.AddComponent<Image>();
        image.transform.SetParent(parent, false);
        Vector2 paddedSize = size;
        paddedSize.y -= verticalPadding;
        image.rectTransform.sizeDelta = paddedSize;        
        image.rectTransform.anchoredPosition = position;
        image.sprite = progressSprite;
        image.color = progressSpriteColor;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Right;

        progressImage = image;            

        GameObject textObject = new GameObject("_progressText");
        TMPro.TextMeshPro text = textObject.AddComponent<TMPro.TextMeshPro>();
        textObject.transform.SetParent(parent, false);
        text.rectTransform.sizeDelta = size;
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.Rotate(0.0f, 180.0f, 0.0f);
        text.sortingOrder = 1;
        text.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
        text.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        text.fontSize = 4.0f;
        text.font = progressFont;
        text.text = "100%";

        progressText = text;

        if (IsCreatingUnitQueueMenu)
        {
            spawnQueue.SetProgressText(text);
            spawnQueue.SetProgressImage(image);
        }
    }  

    [ExecuteInEditMode]
    public void AddQueueButtonType(RTSUnitType unitType)
    {
        queueUnitButtons.Add(unitType);
        RegenerateQueueMenu();
    }

    [ExecuteInEditMode]
    private void AddQueueButton(RTSUnitType unitType)
    {
        if (unitType == RTSUnitType.None)
            return;

        if (!menuParentObject)
        {
            Transform mParent = CreateMenuParent(this.transform);
            buttonsParent = CreateButtonsParent(mParent.transform);
        }

        GenerateQueueButton(unitType, nextButtonPosition, buttonsParent.transform);
        currentButtonColumn++;
        if (currentButtonColumn >= buttonColumns)
        {
            currentButtonColumn = 0;
            currentButtonRow++;
        }
        nextButtonPosition.x = buttonsGenOffset.x - ((buttonSize + spaceBetweenButtons) * currentButtonColumn);
        nextButtonPosition.y = buttonsGenOffset.y - ((buttonSize + spaceBetweenButtons) * currentButtonRow);
    }

    [ExecuteInEditMode]
    public void AddEmptyButton()
    {
        if (!menuParentObject)
        {
            // Transform mParent = CreateMenuParent(this.transform);
            // buttonsParent = CreateButtonsParent(mParent.transform);
            IsCreatingUnitQueueMenu = false;
            GenerateMenu(false);
        }

        GenerateEmptyButton(nextButtonPosition, buttonsParent.transform);
        currentButtonColumn++;
        if (currentButtonColumn >= buttonColumns)
        {
            currentButtonColumn = 0;
            currentButtonRow++;
        }
        nextButtonPosition.x = buttonsGenOffset.x - ((buttonSize + spaceBetweenButtons) * currentButtonColumn);
        nextButtonPosition.y = buttonsGenOffset.y - ((buttonSize + spaceBetweenButtons) * currentButtonRow);
    }

    private void RegenerateQueueMenu()
    {
        if (menuParentObject)
            DestroyImmediate(menuParentObject);

        GenerateQueueMenu(false);
    }

    [ExecuteInEditMode]
    private Image GenerateQueueSlot(Vector3 position, Transform parent)
    {
        GameObject imageObject = new GameObject("_queueSlotImage");
        Image image = imageObject.AddComponent<Image>();
        image.transform.SetParent(parent, false);      
        image.rectTransform.sizeDelta = new Vector2(queueSlotSize, queueSlotSize);
        image.rectTransform.anchoredPosition = position;
        image.sprite = emptyQueueSlotSprite;

        return image;
    }   

    [ExecuteInEditMode]
    private GameObject GenerateQueueButton(RTSUnitType unitType, Vector3 position, Transform parent)
    {
        UnitData typeData = GameMaster.GetUnit(unitType);

        // Button
        GameObject button = new GameObject("_button", typeof(QueueUnitButton));
        button.transform.SetParent(parent, false);
        button.transform.localPosition = position;
        button.name = string.Format("_queue_{0}_Button", unitType.ToString());
        button.transform.localScale = new Vector3(buttonSize, buttonSize, buttonSize);

        QueueUnitButton queueUnitButton = button.GetComponent<QueueUnitButton>();
        queueUnitButton.unitTypeToQueue = unitType;
        queueUnitButton.buttonLockedObject = buttonLockPrefab;

        // Base (child of Button)
        GameObject buttonBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonBase.name = "_base";
        buttonBase.transform.SetParent(button.transform, false);
        buttonBase.transform.localScale = new Vector3(1.0f, 1.0f, 0.15f);
        buttonBase.transform.localPosition = Vector3.zero;
        buttonBase.transform.GetComponent<MeshRenderer>().sharedMaterial = buttonBaseMaterial;

        // Instantiate the resource cost gameobject
        GameObject resourceCost = Instantiate(resourceCostPrefab, Vector3.zero, Quaternion.identity, button.transform);
        resourceCost.transform.localPosition = new Vector3(0.0f, -0.462f, 0.008f);
        resourceCost.transform.localRotation = Quaternion.identity;

        // Popluate the resource cost prefab text objects
        BuildMenuResouceCost cost = resourceCost.GetComponent<BuildMenuResouceCost>();
        cost.woodText.text = typeData.woodCost.ToString();
        cost.goldText.text = typeData.goldCost.ToString();
        cost.grainText.text = typeData.foodCost.ToString();
        cost.stoneText.text = typeData.stoneCost.ToString();

        // Face (child of Button)
        GameObject face = new GameObject("_face", typeof(Interactable), typeof(HoverButton));
        face.transform.SetParent(button.transform, false);
        face.transform.localPosition = new Vector3(0.0f, 0.0f, 0.05f);
        face.transform.localScale = new Vector3(0.85f, 0.85f, 0.15f);
        HoverButton hoverButton = face.GetComponent<HoverButton>();
        hoverButton.localMoveDistance = new Vector3(0, 0, -0.3f);
        face.GetComponent<Interactable>().highlightOnHover = false;

        // Lock (child of Button)
        GameObject buttonLock = Instantiate<GameObject>(buttonLockPrefab);
        buttonLock.name = "_lock";
        buttonLock.transform.SetParent(button.transform, false);
        buttonLock.transform.localPosition = new Vector3(0.0f, 0.0f, 0.13f);
        buttonLock.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        buttonLock.SetActive(false);

        // MovingPart (child of Face)
        GameObject buttonMovingPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonMovingPart.AddComponent<UVCubeMap>();
        buttonMovingPart.name = "_movingPart";
        buttonMovingPart.transform.SetParent(face.transform, false);
        buttonMovingPart.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        buttonMovingPart.transform.localPosition = Vector3.zero;
        buttonMovingPart.transform.GetComponent<MeshRenderer>().sharedMaterial = typeData.worldButtonMaterial;

        hoverButton.movingPart = buttonMovingPart.transform;
        button.transform.localRotation = Quaternion.identity;

        if (Time.time <= 0)
            Destroy(buttonBase.GetComponent<BoxCollider>());
        else
            DestroyImmediate(buttonBase.GetComponent<BoxCollider>());

        return button;
    }

    private void GenerateCancelButton(Transform parent)
    {
        // Base (child of parent)
        GameObject buttonBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonBase.name = "_base";
        buttonBase.transform.SetParent(parent, false);
        buttonBase.transform.localScale = new Vector3(1.0f, 1.0f, 0.15f);
        buttonBase.transform.localPosition = Vector3.zero;
        buttonBase.transform.GetComponent<MeshRenderer>().sharedMaterial = buttonBaseMaterial;

        // Face (child of parent)
        GameObject face = new GameObject("_face", typeof(Interactable), typeof(HoverButton));
        face.transform.SetParent(parent, false);
        face.transform.localPosition = new Vector3(0.0f, 0.0f, 0.05f);
        face.transform.localScale = new Vector3(0.85f, 0.85f, 0.15f);
        HoverButton hoverButton = face.GetComponent<HoverButton>();
        hoverButton.localMoveDistance = new Vector3(0, 0, -0.3f);
        face.GetComponent<Interactable>().highlightOnHover = false;

        spawnQueue.SetCancelButton(hoverButton);
        cancelButton = hoverButton;

        // MovingPart (child of Face)
        GameObject buttonMovingPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonMovingPart.AddComponent<UVCubeMap>();
        buttonMovingPart.name = "_movingPart";
        buttonMovingPart.transform.SetParent(face.transform, false);
        buttonMovingPart.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        buttonMovingPart.transform.localPosition = Vector3.zero;
        buttonMovingPart.transform.GetComponent<MeshRenderer>().sharedMaterial = cancelButtonMaterial;

        hoverButton.movingPart = buttonMovingPart.transform;
        //button.transform.localRotation = Quaternion.identity;

        if (Time.time <= 0)
            Destroy(buttonBase.GetComponent<BoxCollider>());
        else
            DestroyImmediate(buttonBase.GetComponent<BoxCollider>());
    }


    // private void HookIntoEvents()
    // {
    //     HoverButton[] hoverButtons = GetComponentsInChildren<HoverButton>(true);
    //     if (hoverButtons.Length > 0)
    //         foreach (HoverButton hButton in hoverButtons)
    //         {
    //             hButton.onButtonDown.AddListener(OnButtonDown);
    //             hButton.onButtonUp.AddListener(OnButtonUp);
    //         }

    //     if (cancelButton)
    //         cancelButton.onButtonDown.AddListener(OnCancelButtonDown);
    // }

    // private void CleanupEvents()
    // {
    //     HoverButton[] hoverButtons = GetComponentsInChildren<HoverButton>(true);
    //     if (hoverButtons.Length > 0)
    //         foreach (HoverButton hButton in hoverButtons)
    //         {
    //             hButton.onButtonDown.RemoveListener(OnButtonDown);
    //             hButton.onButtonUp.RemoveListener(OnButtonUp);
    //         }

    //     if (cancelButton)
    //         cancelButton.onButtonDown.RemoveListener(OnCancelButtonDown);
    // }

    // void OnDestroy()
    // {
    //     CleanupEvents();
    // }

   

    [ExecuteInEditMode]
    private GameObject GenerateEmptyButton(Vector3 position, Transform parent)
    {
        // Button
        GameObject button = new GameObject("_button");
        button.transform.SetParent(parent, false);
        button.transform.localPosition = position;
        button.transform.localScale = new Vector3(buttonSize, buttonSize, buttonSize);

        // Base (child of Button)
        GameObject buttonBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonBase.name = "_base";
        buttonBase.transform.parent = button.transform;
        buttonBase.transform.localScale = new Vector3(1.0f, 1.0f, 0.15f);
        buttonBase.transform.localPosition = Vector3.zero;
        buttonBase.transform.GetComponent<MeshRenderer>().sharedMaterial = buttonBaseMaterial;

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
        face.transform.parent = button.transform;
        face.transform.localPosition = new Vector3(0.0f, 0.0f, 0.05f);
        face.transform.localScale = new Vector3(0.85f, 0.85f, 0.15f);
        HoverButton hoverButton = face.GetComponent<HoverButton>();
        hoverButton.localMoveDistance = new Vector3(0, 0, -0.3f);
        face.GetComponent<Interactable>().highlightOnHover = false;

        // Lock (child of Button)
        GameObject buttonLock = Instantiate<GameObject>(buttonLockPrefab);
        buttonLock.name = "_lock";
        buttonLock.transform.parent = button.transform;
        buttonLock.transform.localPosition = new Vector3(0.0f, 0.0f, 0.13f);
        buttonLock.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        buttonLock.SetActive(false);

        // MovingPart (child of Face)
        GameObject buttonMovingPart = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonMovingPart.AddComponent<UVCubeMap>();
        buttonMovingPart.name = "_movingPart";
        buttonMovingPart.transform.SetParent(face.transform);
        buttonMovingPart.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        buttonMovingPart.transform.localPosition = Vector3.zero;
        //buttonMovingPart.transform.GetComponent<MeshRenderer>().sharedMaterial = typeData.worldButtonMaterial;

        hoverButton.movingPart = buttonMovingPart.transform;
        button.transform.localRotation = Quaternion.identity;

        if (Time.time <= 0)
            Destroy(buttonBase.GetComponent<BoxCollider>());
        else
            DestroyImmediate(buttonBase.GetComponent<BoxCollider>());

        return button;
    }

    

    // public void SetUnitRallyWaypoint(Vector3 position)
    // {
    //     unitRallyWaypoint.transform.position = position;
    // }

    // void Update()
    // {
    //     UpdateUnitSpawnQueue();
    // }

    // public void OnButtonDown(Hand hand)
    // {
    //     audioSource.PlayOneShot(onButtonDownAudio);

    //     QueueUnitButton queueUnitButton = hand.hoveringInteractable.GetComponentInParent<QueueUnitButton>();
    //     if (queueUnitButton)
    //     {
    //         QueueUnit(queueUnitButton.unitTypeToQueue);
    //     }

    // }

    // public void OnButtonUp(Hand hand)
    // {
    //     audioSource.PlayOneShot(onButtonUpAudio);       
    // }

    // public void OnCancelButtonDown(Hand hand)
    // {
    //     DequeueUnit();
    // }

    // public bool QueueLastUnitQueued() { return QueueUnit(lastUnitQueued); }

    // public bool QueueUnit(RTSUnitType unitTypeToQueue)
    // {
    //     // TODO: Reenable this later
    //     // if (damageable.GetAttributePercent(Attributes.HEALTH) < 1.0f)
    //     //     return;

    //     if (unitSpawnQueue.Count >= structure.buildingData.maxUnitQueueSize)
    //         return false;

    //     if (structure.IsSameFaction(playerManager.factionId) &&
    //         !playerManager.CanQueueUnit(unitTypeToQueue))
    //         return false;

    //     UnitData unitData = GameMaster.GetUnit(unitTypeToQueue);
    //     playerManager.DeductUnitQueueCostFromStockpile(unitData);
    //     unitSpawnQueue.AddLast(unitData);

    //     // Debug.Log("Queued " + unitData.unitType);

    //     return true;
    // }

    // private void UpdateUnitSpawnQueue()
    // {
    //     if (unitSpawnQueue.Count > 0)
    //     {
    //         timeElapsed += Time.deltaTime;
    //         progressImage.fillAmount = (timeElapsed / unitSpawnQueue.First.Value.queueTime);
    //         float progressPercent = UnityEngine.Mathf.Round(progressImage.fillAmount * 100);
    //         progressText.text = progressPercent.ToString() + "%";

    //         if (timeElapsed >= unitSpawnQueue.First.Value.queueTime)
    //         {
    //             SpawnUnit();
    //             timeElapsed = 0.0f;
    //             unitSpawnQueue.RemoveFirst();
    //             progressImage.fillAmount = 0;
    //             progressImage.enabled = false;
    //             progressText.enabled = false;
    //         }
    //         else
    //         {
    //             progressImage.enabled = true;
    //             progressText.enabled = true;
    //         }

    //         RefreshQueueImages();
    //     }
    //     else
    //     {
    //         timeElapsed = 0.0f;
    //     }
    // }

    // public void DequeueUnit()
    // {
    //     if (unitSpawnQueue.Count <= 0)
    //         return;

    //     else if (unitSpawnQueue.Count == 1)
    //     {
    //         playerManager.RemoveFromQueueCount(unitSpawnQueue.Last.Value.populationCost);
    //         unitSpawnQueue.RemoveLast();
    //         progressImage.fillAmount = 0;
    //         progressImage.enabled = false;
    //         progressText.enabled = false;
    //         RefreshQueueImages();
    //     }
    //     else
    //     {
    //         playerManager.RemoveFromQueueCount(unitSpawnQueue.Last.Value.populationCost);
    //         unitSpawnQueue.RemoveLast();
    //     }
    // }

    // private void SpawnUnit()
    // {
    //     if (unitSpawnQueue.First.Value.prefab)
    //     {
    //         GameObject unitGameObject = Instantiate(unitSpawnQueue.First.Value.prefab, unitSpawnPoint.transform.position, Quaternion.identity);
    //         Unit unit = unitGameObject.GetComponent<Unit>();
    //         unit.rtsUnitType = unitSpawnQueue.First.Value.unitType;
    //         unit.factionId = structure.factionId;

    //         // ! Dsabled, none of this works for rally points anymore.
    //         //unit.SyncPosition();
    //         //unit.GotoForced(World.ToWorldSpace(unitRallyWaypoint.position));
    //         //unit.LockPath();

    //         // Debug.Log("Spawned " + unit.rtsUnitType + ".");
    //     }
    //     else
    //         Debug.Log(string.Format("Spawn {0} failed. Missing prefabToSpawn.", unitSpawnQueue.First.Value.unitType));
    // }

    // private void RefreshQueueImages()
    // {
    //     foreach (UnityEngine.UI.Image image in queueSlotImages)
    //     {
    //         // Clearing override sprite reenables the original
    //         image.overrideSprite = null;
    //     }

    //     int i = 0;
    //     foreach (UnitData unitData in unitSpawnQueue)
    //     {
    //         queueSlotImages[i].overrideSprite = unitData.queueImage;
    //         i++;

    //         if (i >= unitSpawnQueue.Count)
    //             break;
    //     }
    // }
}
