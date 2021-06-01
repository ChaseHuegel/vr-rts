using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;
using Valve.VR.InteractionSystem;

public class WallGate : MonoBehaviour
{
    public OpenCondition openConditions = OpenCondition.None;

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
        None,
        Friendly,
        Enemy,
        Ally,
    }
}
