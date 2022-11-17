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
    public Transform UnitSpawnPoint { get => unitSpawnPoint; } 
    private Transform unitRallyWaypoint;
    private Cell unitRallyPointCell;
    public Cell UnitRallyPointCell { get => unitRallyPointCell; }
    private AudioClip onButtonDownAudio;
    private AudioClip onButtonUpAudio;
    private Image[] queueSlotImages;
    private TMPro.TMP_Text progressText;
    private Image progressImage;
    private GameObject buttonsParent;
    private HoverButton cancelButton;

    //=========================================================================
    private float timeElapsed = 0.0f;
    private LinkedList<TechBase> queue = new LinkedList<TechBase>();

    //=========================================================================
    // Cached references
    private Structure structure;
    public Structure Structure { get => structure; }

    private Damageable damageable;

    //=========================================================================
    private RTSUnitType lastUnitQueued;

    public void Initialize()
    {
        if (!unitSpawnPoint)
        {
            Structure structure = GetComponentInParent<Structure>();
            if (structure)
            {
                unitSpawnPoint = structure.transform;
                //Debug.LogWarning("UnitSpawnPoint not set, using structure transform.", this);

                if (!structure.Faction)
                    Debug.LogError("Structure missing faction.", this);
            }
            else
            {
                Debug.LogError("UnitSpawnPoint not set, no structure found.", this);
            }
        }

        if (unitSpawnPoint && unitRallyWaypoint)
            SetUnitRallyPointPosition(unitSpawnPoint.position);

        if (!(damageable = gameObject.GetComponentInParent<Damageable>()))
            Debug.LogError("Missing damageable component in parent.", this);

        if (!(structure = gameObject.GetComponentInParent<Structure>()))
            Debug.LogError("Missing structure component in parent.", this);

        HookIntoEvents();

        // QueueUnitButton firstButton = GetComponentInChildren<QueueUnitButton>(true);
        // if (firstButton)
        //     lastUnitQueued = firstButton.techToQueue;
    }

    void Update() { UpdateUnitSpawnQueue(); }

    public void OnHoverButtonDown(Hand hand)
    {
        QueueUnitButton queueUnitButton = hand.hoveringInteractable?.GetComponentInParent<QueueUnitButton>();
        if (queueUnitButton)
        {
            if (queueUnitButton.IsButtonUnlocked && queueUnitButton.IsButtonEnabled)
                QueueTech(queueUnitButton.techToQueue);                
        }

        PlayerManager.Instance.PlayAudioAtHeadSource(onButtonDownAudio);
    }

    public void OnHoverButtonUp(Hand hand)
    {
        PlayerManager.Instance.PlayAudioAtHeadSource(onButtonUpAudio);
    }

    public void OnCancelButtonDown(Hand hand)
    {
        PlayerManager.Instance.PlayAudioAtHeadSource(onButtonDownAudio);
        DequeueUnit(); 
    }

    //public bool QueueLastUnitQueued() { return QueueUnit(lastUnitQueued); }

    public bool QueueTech(TechBase tech)
    {
        if (queue.Count >= structure.buildingData.maxUnitQueueSize)
        {
            Debug.Log("Queue size >= maxUnitQueueSize.");
            return false;
        }

        if (!structure.Faction.IsSameFaction(PlayerManager.Instance.faction))
        {
            Debug.Log("Faction not the same.");
            return false;
        }

        //UnitData unitData = GameMaster.GetUnit(tech);

        if (tech.singleUse && queue.Contains(tech))
        {
            Debug.Log("Single use tech.");
            return false;
        }

        if (!PlayerManager.Instance.CanQueueTech(tech))
        {
            Debug.Log("Can't queue tech.");
            return false;
        }

        PlayerManager.Instance.DeductTechResourceCost(tech);
        queue.AddLast(tech);
        RefreshQueueImages();
        return true;
    }

    private void UpdateUnitSpawnQueue()
    {
        if (queue.Count > 0)
        {
            timeElapsed += Time.deltaTime;
            progressImage.fillAmount = (timeElapsed / queue.First.Value.queueResearchTime);
            float progressPercent = UnityEngine.Mathf.Round(progressImage.fillAmount * 100);
            progressText.text = progressPercent.ToString() + "%";

            if (timeElapsed >= queue.First.Value.queueResearchTime)
            {            
                queue.First.Value.Execute(this);
                timeElapsed = 0.0f;
                queue.RemoveFirst();
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
        if (queue.Count <= 0)
            return;

        else if (queue.Count == 1)
        {
            PlayerManager.Instance.RemoveFromQueueCount(queue.Last.Value.populationCost);
            queue.RemoveLast();
            progressImage.fillAmount = 0;
            progressImage.enabled = false;
            progressText.enabled = false;            
        }
        else
        {
            PlayerManager.Instance.RemoveFromQueueCount(queue.Last.Value.populationCost);
            queue.RemoveLast();
        }
        RefreshQueueImages();
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
        foreach (TechBase techBase in queue)
        {
            queueSlotImages[i].overrideSprite = techBase.worldQueueImage;
            i++;

            if (i >= queue.Count)
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
                        
                    hButton.onButtonDown.AddListener(OnHoverButtonDown);
                    hButton.onButtonUp.AddListener(OnHoverButtonUp);
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
                    hButton.onButtonDown.RemoveListener(OnHoverButtonDown);
                    hButton.onButtonUp.RemoveListener(OnHoverButtonUp);                    
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
