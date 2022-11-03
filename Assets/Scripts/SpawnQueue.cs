using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Audio;
using Swordfish.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

public class SpawnQueue : MonoBehaviour
{
    [Header("Unit Spawn Queue Settings")]    
    public float unitRallyWaypointRadius;

    private Transform unitSpawnPoint;
    private Transform unitRallyWaypoint;
    private Cell unitRallyPointCell;
    private AudioClip onButtonDownAudio;
    private AudioClip onButtonUpAudio;
    private Image[] queueSlotImages;
    private TMPro.TMP_Text progressText;
    private Image progressImage;
    private GameObject buttonsParent;
    private HoverButton cancelButton;

    //=========================================================================
    private float timeElapsed = 0.0f;
    private LinkedList<UnitData> unitSpawnQueue = new LinkedList<UnitData>();

    //=========================================================================
    // Cached references
    private Structure structure;
    private Damageable damageable;
    private AudioSource audioSource;
    private PlayerManager playerManager;

    //=========================================================================
    private RTSUnitType lastUnitQueued;

    void Start()
    {
        playerManager = PlayerManager.instance;
        //Initialize();
    }

    public void Initialize()
    {
        if (!unitSpawnPoint)
        {
            Structure structure = GetComponentInParent<Structure>();
            if (structure)
            {
                unitSpawnPoint = structure.transform;
                Debug.Log("UnitSpawnPoint not set, using structure transform.", this);
            }
            else
            {
                Debug.Log("UnitSpawnPoint not set and no structure found.", this);
            }
        }

        SetUnitRallyPointPosition(unitSpawnPoint.position);

        if (!(damageable = gameObject.GetComponentInParent<Damageable>()))
            Debug.Log("Missing damageable component in parent.", this);

        if (!(structure = gameObject.GetComponentInParent<Structure>()))
            Debug.Log("Missing structure component in parent.", this);

        if (!(audioSource = gameObject.GetComponentInParent<AudioSource>()))
            Debug.Log("Missing audiosource component in parent.", this);

        HookIntoEvents();

        QueueUnitButton firstButton = GetComponentInChildren<QueueUnitButton>(true);
        if (firstButton)
            lastUnitQueued = firstButton.unitTypeToQueue;
    }

    void Update() { UpdateUnitSpawnQueue(); }

    public void OnButtonDown(Hand hand)
    {
        audioSource.PlayOneShot(onButtonDownAudio);

        QueueUnitButton queueUnitButton = hand.hoveringInteractable.GetComponentInParent<QueueUnitButton>();
        if (queueUnitButton)
            QueueUnit(queueUnitButton.unitTypeToQueue);

    }

    public void OnButtonUp(Hand hand) { audioSource.PlayOneShot(onButtonUpAudio); }

    public void OnCancelButtonDown(Hand hand)
    {
        audioSource.PlayOneShot(onButtonDownAudio);
        DequeueUnit(); 
    }

    public bool QueueLastUnitQueued() { return QueueUnit(lastUnitQueued); }

    public bool QueueUnit(RTSUnitType unitTypeToQueue)
    {
        if (unitSpawnQueue.Count >= structure.buildingData.maxUnitQueueSize)
            return false;

        if (!structure.Faction.IsSameFaction(playerManager.faction))
            return false;

        UnitData unitData = GameMaster.GetUnit(unitTypeToQueue);

        if (!playerManager.CanQueueUnit(unitData))
            return false;

        playerManager.DeductUnitQueueCostFromStockpile(unitData);
        unitSpawnQueue.AddLast(unitData);
        RefreshQueueImages();
        return true;
    }

    private void UpdateUnitSpawnQueue()
    {
        if (unitSpawnQueue.Count > 0)
        {
            timeElapsed += Time.deltaTime;
            progressImage.fillAmount = (timeElapsed / unitSpawnQueue.First.Value.queueResearchTime);
            float progressPercent = UnityEngine.Mathf.Round(progressImage.fillAmount * 100);
            progressText.text = progressPercent.ToString() + "%";

            if (timeElapsed >= unitSpawnQueue.First.Value.queueResearchTime)
            {
                SpawnUnit();
                timeElapsed = 0.0f;
                unitSpawnQueue.RemoveFirst();
                progressImage.fillAmount = 0;
                progressImage.enabled = false;
                progressText.enabled = false;
                RefreshQueueImages();              
            }
            else
            {
                progressImage.enabled = true;
                progressText.enabled = true;
            }            
        }
        else
            timeElapsed = 0.0f;
    }

    public void DequeueUnit()
    {
        if (unitSpawnQueue.Count <= 0)
            return;

        else if (unitSpawnQueue.Count == 1)
        {
            playerManager.RemoveFromQueueCount(unitSpawnQueue.Last.Value.populationCost);
            unitSpawnQueue.RemoveLast();
            progressImage.fillAmount = 0;
            progressImage.enabled = false;
            progressText.enabled = false;            
        }
        else
        {
            playerManager.RemoveFromQueueCount(unitSpawnQueue.Last.Value.populationCost);
            unitSpawnQueue.RemoveLast();
        }
        RefreshQueueImages();
    }


    private void SpawnUnit()
    {
        if (unitSpawnQueue.First.Value.worldPrefab)
        {
            GameObject unitGameObject = Instantiate(unitSpawnQueue.First.Value.worldPrefab, unitSpawnPoint.transform.position, Quaternion.identity);
            UnitV2 unit = unitGameObject.GetComponent<UnitV2>();
            unit.Faction = structure.Faction;
            unit.SetUnitType(unitSpawnQueue.First.Value.unitType);
            unit.IssueSmartOrder(unitRallyPointCell);
        }
        else
            Debug.Log(string.Format("Spawn {0} failed. Missing prefabToSpawn.", unitSpawnQueue.First.Value.unitType));
    }

    private void ResetLastQueueImage()
    {
        queueSlotImages[queueSlotImages.Length].overrideSprite = null;
    }

    private void RefreshQueueImages()
    {
        foreach (UnityEngine.UI.Image image in queueSlotImages)
        {
            // Clearing override sprite reenables the original
            image.overrideSprite = null;
        }

        int i = 0;
        foreach (UnitData unitData in unitSpawnQueue)
        {
            queueSlotImages[i].overrideSprite = unitData.worldQueueImage;
            i++;

            if (i >= unitSpawnQueue.Count)
                break;
        }
    }

    //=========================================================================
    // Menu Generation Utilities
    //-------------------------------------------------------------------------

    /// <summary>
    /// Initializes internal queue slot image array.
    /// </summary>
    /// <param name="count">The new size of the array.</param>
    public void SetSpawnQueueSlotCount(byte count)
    {
        if (count >= 0) queueSlotImages = new Image[count];
    }

    public void SetQueueSlotImage(Image image, byte index)
    {
        if (image && index >= 0) queueSlotImages[index] = image;
    }

    public void SetUnitSpawnPointTransform(Transform target)
    {
        unitSpawnPoint = target;
    }

    public void SetUnitRallyPointTransform(Transform target)
    {
        unitRallyWaypoint = target;
        unitRallyPointCell = World.at(World.ToWorldCoord(unitRallyWaypoint.position));
    }

    public void SetUnitRallyPointPosition(Vector3 position)
    {
        unitRallyWaypoint.position = position;
        unitRallyPointCell = World.at(World.ToWorldCoord(unitRallyWaypoint.position));
    }

    public void SetCancelButton(HoverButton button)
    {
        if (button) cancelButton = button;
    }

    public void SetButtonsParentObject(GameObject gameObject)
    {
        if (gameObject) buttonsParent = gameObject;
    }

    public void SetButtonDownAudio(AudioClip clip)
    {
        onButtonDownAudio = clip;
    }

    public void SetButtonUpAudio(AudioClip clip)
    {
        onButtonUpAudio = clip;
    }

    public void SetProgressImage(Image image)
    {
        progressImage = image;
    }

    public void SetProgressText(TMPro.TextMeshPro text)
    {
        progressText = text;
    }

    //=========================================================================

    private void HookIntoEvents()
    {
        if (buttonsParent)
        {
            HoverButton[] hoverButtons = buttonsParent.GetComponentsInChildren<HoverButton>(true);
            if (hoverButtons.Length > 0)
                foreach (HoverButton hButton in hoverButtons)
                {
                    if (hButton.onButtonDown == null)
                        hButton.onButtonDown = new HandEvent();

                    if (hButton.onButtonUp == null)
                        hButton.onButtonUp = new HandEvent();
                        
                    hButton.onButtonDown.AddListener(OnButtonDown);
                    hButton.onButtonUp.AddListener(OnButtonUp);
                }
        }
        else
            Debug.LogWarning("HookIntoEvents: buttonsParent not found.", this);

        if (cancelButton)
        {
            if (cancelButton.onButtonDown == null)
                cancelButton.onButtonDown = new HandEvent();

            cancelButton.onButtonDown.AddListener(OnCancelButtonDown);
        }
        else
            Debug.LogWarning("HookIntoEvents: cancelButton not found.", this);
    }

    private void CleanupEvents()
    {
        if (buttonsParent)
        {
            HoverButton[] hoverButtons = buttonsParent.GetComponentsInChildren<HoverButton>(true);
            if (hoverButtons.Length > 0)
                foreach (HoverButton hButton in hoverButtons)
                {
                    hButton.onButtonDown.RemoveListener(OnButtonDown);
                    hButton.onButtonUp.RemoveListener(OnButtonUp);
                }
        }
        else
            Debug.LogWarning("CleanupEvents: buttonsParent not found.", this);

        if (cancelButton)
            cancelButton.onButtonDown.RemoveListener(OnCancelButtonDown);
        else
            Debug.LogWarning("CleanupEvents: cancelButton not found.", this);
    }

    void OnDestroy()
    {
        CleanupEvents();
    }
}
