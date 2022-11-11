using UnityEngine;

public class WallGate : MonoBehaviour
{
    public OpenCondition openConditions;

    private PlayerManager playerManager;
    private Structure structure;
    private Animator animator;

    // [InspectorButton("ToggleDoors")]
    // public bool toggleDoors;

    bool isOpen = false;

    void Start()
    {
        playerManager = PlayerManager.Instance;
        structure = GetComponentInParent<Structure>();
        animator = GetComponentInChildren<Animator>();

        // TODO: Switch to rotated/fixed prefab?
        // Stretch if diagonal        
        float angle = Mathf.Round(transform.eulerAngles.y);
        if (angle == 45.0f || angle == 135.0f || angle == 225.0f || angle == 315.0f)
            transform.localScale += new Vector3(0.0f, 0.01199419f, 0.0f);
    }

    void OnTriggerEnter(Collider collider)
    {
        UnitV2 unit = collider.gameObject.GetComponent<UnitV2>();
        if (unit)
        {
            // Friendly
            if (unit.Faction.IsSameFaction(playerManager.faction) && openConditions.HasFlag(OpenCondition.Friendly))
            {
                OpenDoors();
                return;
            }

            // Enemy
            if (!unit.Faction.IsAllied(playerManager.faction) && openConditions.HasFlag(OpenCondition.Enemy))
            {
                OpenDoors();
                return;
            }

            //  Allied
            if (unit.Faction.IsAllied(playerManager.faction) && openConditions.HasFlag(OpenCondition.Ally))
            {
                OpenDoors();
                return;
            }
        }
    }

    void OnTriggerExit(Collider collider)
    {
        UnitV2 unit = collider.gameObject.GetComponentInParent<UnitV2>();
        if (unit && isOpen)
            CloseDoors();
    }

    public void OpenDoors()
    {
        animator.SetBool("DoorOpen", true);
        structure.UnbakeFromGrid();
        isOpen = true;
    }

    public void CloseDoors()
    {
        animator.SetBool("DoorOpen", false);
        structure.BakeToGrid();
        isOpen = false;
    }

    public void ToggleDoors()
    {
        if (isOpen)
            CloseDoors();
        else
            OpenDoors();
    }

    [System.Flags]
    public enum OpenCondition
    {
        Friendly = 1,
        Enemy = 2,
        Ally = 4,
    }
}
