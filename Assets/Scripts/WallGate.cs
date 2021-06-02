using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;
using Valve.VR.InteractionSystem;

public class WallGate : MonoBehaviour
{
    public OpenCondition openConditions;

    private PlayerManager playerManager;
    private Structure structure;
    private Faction faction;
    private Animator animator;

    [InspectorButton("ToggleDoors")]
    public bool toggleDoors;

    bool isOpen = false;

    // Start is called before the first frame update
    void Start()
    {
        playerManager = PlayerManager.instance;
        structure = GetComponentInParent<Structure>();
        faction = structure.GetFaction();
        animator = GetComponentInChildren<Animator>();

        // TODO: Switch to rotated/fixed prefab?
        // Stretch if diagonal        
        float angle = Mathf.Round(transform.eulerAngles.y);
        if (angle == 45.0f || angle == 135.0f || angle == 225.0f || angle == 315.0f)
            transform.localScale += new Vector3(0.0f, 0.01199419f, 0.0f);
    }

    void OnTriggerEnter(Collider collider)
    {
        // Hand hand = collider.GetComponentInParent<Hand>();
        // if (hand)
        // {
        //     ToggleDoors();
        //     return;
        // }
        
        // if (openConditions.HasFlag(OpenCondition.None))
        //     return;

        Unit unit = collider.gameObject.GetComponentInParent<Unit>();
        if (unit)
        {
            // Friendly
            if (unit.IsSameFaction(playerManager.factionId) && openConditions.HasFlag(OpenCondition.Friendly))
            {
                OpenDoors();
                return;
            }

            // Enemy
            if (!unit.IsSameFaction(playerManager.factionId) && openConditions.HasFlag(OpenCondition.Enemy))
            {
                OpenDoors();
                return;
            }

            // Ally
            // if (faction.IsAllied(unit.GetFaction()) && openConditions.HasFlag(OpenCondition.Ally))
            // {
            //     OpenDoors();
            //     return;
            // }
        }
    }

    void OnTriggerExit(Collider collider)
    {
        Unit unit = collider.gameObject.GetComponentInParent<Unit>();
        if (unit && isOpen)
            CloseDoors();
    }

    private void OpenDoors()
    {
        animator.SetTrigger("Open");
        structure.UnbakeFromGrid();
        isOpen = true;
    }

    private void CloseDoors()
    {
        animator.SetTrigger("Close");
        structure.BakeToGrid();
        isOpen = false;
    }

    private void ToggleDoors()
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
