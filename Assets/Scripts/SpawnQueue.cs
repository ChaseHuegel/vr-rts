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
    private TechBase lastTechQueued;

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

        QueueUnitButton firstButton = GetComponentInChildren<QueueUnitButton>(true);
        if (firstButton)
            lastTechQueued = firstButton.techToQueue;
    }

    void Update() { UpdateUnitSpawnQueue(); }

    public bool QueueLastTechQueued() { return QueueTech(lastTechQueued); }

    public bool QueueTech(TechBase tech)
    {
        if (queue.Count >= structure.buildingData.maxUnitQueueSize)
        {
            Debug.Log("Queue Failed: Queue size >= maxUnitQueueSize of structure.");
            return false;
        }

        if (!structure.Faction.IsSameFaction(PlayerManager.Instance.faction))
        {
            Debug.Log("Queue Failed: Wrong faction.");
            return false;
        }

        //UnitData unitData = GameMaster.GetUnit(tech);

        if (!(tech is UnitData) && queue.Contains(tech))
        {
            Debug.Log("Queue Failed: Tech already queued.");
            return false;
        }

        if (!PlayerManager.Instance.TryToQueueTech(tech))
        {
            Debug.Log("Queue Failed: Can't queue tech.");
            return false;
        }        
        
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
            queueSlotImages[i].overrideSprite = Sprite.Create(techBase.worldQueueTexture, new Rect(0f, 0f, techBase.worldQueueTexture.width, techBase.worldQueueTexture.height), new Vector2(0.5f, 0.5f), 100.0f, 1, SpriteMeshType.Tight, Vector4.zero, true);
            i++;

            if (i >= queue.Count)
                break;
        }
    }

    //=========================================================================
    // Generation Access
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

    public void SetProgressImage(Image image)
    {
        progressImage = image;
    }

    public void SetProgressText(TMPro.TextMeshPro text)
    {
        progressText = text;
    }    
}
