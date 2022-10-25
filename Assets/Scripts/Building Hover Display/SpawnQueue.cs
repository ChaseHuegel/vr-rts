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
    public Transform unitSpawnPoint;
    public Transform unitRallyWaypoint;
    private Cell unitRallyPointCell;
    public float unitRallyWaypointRadius;

    [Header("Generated Settings")]
    public AudioClip onButtonDownAudio;
    public AudioClip onButtonUpAudio;
    public Image[] queueSlotImages;
    public TMPro.TMP_Text progressText;
    public Image progressImage;
    public GameObject buttonsParent;
    public GameObject menuParentObject;
    public HoverButton cancelButton;

    //=========================================================================
    protected float timeElapsed = 0.0f;
    protected LinkedList<UnitData> unitSpawnQueue = new LinkedList<UnitData>();

    //=========================================================================
    // Cached references
    private Structure structure;
    private Damageable damageable;
    protected AudioSource audioSource;
    private PlayerManager playerManager;

    //=========================================================================
    private RTSUnitType lastUnitQueued;

    void Start()
    {
        playerManager = PlayerManager.instance;

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

        SetUnitRallyWaypoint(unitSpawnPoint.position);

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

    public void OnCancelButtonDown(Hand hand) { DequeueUnit(); }

    public bool QueueLastUnitQueued() { return QueueUnit(lastUnitQueued); }

    public bool QueueUnit(RTSUnitType unitTypeToQueue)
    {
        if (unitSpawnQueue.Count >= structure.buildingData.maxUnitQueueSize)
            return false;

        if (!structure.Faction.IsSameFaction(playerManager.faction) || !playerManager.CanQueueUnit(unitTypeToQueue))
            return false;

        UnitData unitData = GameMaster.GetUnit(unitTypeToQueue);
        playerManager.DeductUnitQueueCostFromStockpile(unitData);
        unitSpawnQueue.AddLast(unitData);
        return true;
    }

    private void UpdateUnitSpawnQueue()
    {
        if (unitSpawnQueue.Count > 0)
        {
            timeElapsed += Time.deltaTime;
            progressImage.fillAmount = (timeElapsed / unitSpawnQueue.First.Value.queueTime);
            float progressPercent = UnityEngine.Mathf.Round(progressImage.fillAmount * 100);
            progressText.text = progressPercent.ToString() + "%";

            if (timeElapsed >= unitSpawnQueue.First.Value.queueTime)
            {
                SpawnUnit();
                timeElapsed = 0.0f;
                unitSpawnQueue.RemoveFirst();
                progressImage.fillAmount = 0;
                progressImage.enabled = false;
                progressText.enabled = false;
            }
            else
            {
                progressImage.enabled = true;
                progressText.enabled = true;
            }

            RefreshQueueImages();
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
            RefreshQueueImages();
        }
        else
        {
            playerManager.RemoveFromQueueCount(unitSpawnQueue.Last.Value.populationCost);
            unitSpawnQueue.RemoveLast();
        }
    }

    private void SpawnUnit()
    {
        if (unitSpawnQueue.First.Value.prefab)
        {
            GameObject unitGameObject = Instantiate(unitSpawnQueue.First.Value.prefab, unitSpawnPoint.transform.position, Quaternion.identity);
            UnitV2 unit = unitGameObject.GetComponent<UnitV2>();
            unit.Faction = structure.Faction;
            unit.SetUnitType(unitSpawnQueue.First.Value.unitType);
            unit.OrderGoTo(unitRallyPointCell);
        }
        else
            Debug.Log(string.Format("Spawn {0} failed. Missing prefabToSpawn.", unitSpawnQueue.First.Value.unitType));
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
            queueSlotImages[i].overrideSprite = unitData.queueImage;
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

    public void SetUnitRallyWaypoint(Vector3 position)
    {
        unitRallyWaypoint.position = position;
        unitRallyPointCell = World.at(World.ToWorldCoord(unitRallyWaypoint.position));
    }

    public void SetCancelButton(HoverButton button)
    {
        if (button) cancelButton = button;
    }

    public void SetMenuParentObject(GameObject gameObject)
    {
        if (gameObject) menuParentObject = gameObject;
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
                    hButton.onButtonDown.AddListener(OnButtonDown);
                    hButton.onButtonUp.AddListener(OnButtonUp);
                }
        }
        else
            Debug.Log("buttonsParent not found.", this);

        if (cancelButton)
            cancelButton.onButtonDown.AddListener(OnCancelButtonDown);
        else
            Debug.Log("cancelButton not found.", this);
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
            Debug.Log("buttonsParent not found.", this);

        if (cancelButton)
            cancelButton.onButtonDown.RemoveListener(OnCancelButtonDown);
        else
            Debug.Log("cancelButton not found.", this);
    }

    void OnDestroy()
    {
        CleanupEvents();
    }
}
