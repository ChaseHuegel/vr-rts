using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using Valve.VR.InteractionSystem;

[Serializable]
public class BuildMenuTab : MonoBehaviour
{
    
    public float horzontalButtonSpacing = 71.0f;
    public float verticalButtonSpacing = 96.0f;
    public int maximumNumberOfColumns = 3;
    public GameObject resourceCostPrefab;
    public Material slotEnabledMaterial;
    public Material slotDisabledMaterial;
    public RTSBuildingType[] Buttons;    

    void Awake()
    {
        // if (transform.childCount <= 0)
        //     Generate();
        BuildMenuSlot.disabledMat = slotDisabledMaterial;
        BuildMenuSlot.enabledMat = slotEnabledMaterial;
    }

    [ExecuteInEditMode]
    public void Generate()
    {
        Vector3 origin = Vector3.zero;
        origin.x = (maximumNumberOfColumns - 1) * horzontalButtonSpacing * -0.5f;
        float rows = Mathf.Ceil((float)Buttons.Length / (float)maximumNumberOfColumns);
        origin.y = (verticalButtonSpacing * -0.5f);
        origin.y += (verticalButtonSpacing * rows) * 0.5f;

        int i = 0;

        int row = 0;
        int column = 0;
        float slotPositionX = origin.x;
        float slotPositionY = origin.y;
        
        DestroyChildren();
        
        foreach ( RTSBuildingType rtsBuildingType in Buttons )
        {
            if (rtsBuildingType != RTSBuildingType.None)
            {
                // Create the button slot gameobject
                GameObject slotObject = new GameObject("_slot_" + i);
                slotObject.transform.SetParent(this.gameObject.transform);

                slotObject.transform.localPosition = new Vector3(slotPositionX, slotPositionY, origin.z);
                slotObject.transform.Rotate(0, 90, -90);
                slotObject.AddComponent<Interactable>();
                slotObject.GetComponent<Interactable>().highlightOnHover = false;
                
                // Set layer
                slotObject.layer = LayerMask.NameToLayer("UI");

                // Add components needed
                BuildMenuSlot buildMenuSlot = slotObject.AddComponent<BuildMenuSlot>();
                SphereCollider sphereCollider = slotObject.AddComponent<SphereCollider>();
                sphereCollider.radius = 40.0f;

                // Instantiate the resource cost gameobject
                GameObject resourceCost = Instantiate(resourceCostPrefab, Vector3.zero, Quaternion.identity, slotObject.transform);
                resourceCost.transform.localPosition = new Vector3(38.0f, 3.0f, 0f);
                resourceCost.transform.localRotation = Quaternion.identity;

                // Fetch and set the building type data from the database
                buildMenuSlot.rtsTypeData = GameMaster.GetBuilding(rtsBuildingType);
                
                // Popluate the resource cost prefab text objects
                BuildMenuResouceCost cost = resourceCost.GetComponent<BuildMenuResouceCost>();
                cost.woodText.text = buildMenuSlot.rtsTypeData.woodCost.ToString();
                cost.goldText.text = buildMenuSlot.rtsTypeData.goldCost.ToString();
                cost.grainText.text = buildMenuSlot.rtsTypeData.grainCost.ToString();
                cost.stoneText.text = buildMenuSlot.rtsTypeData.stoneCost.ToString();

                // Create/Instatiate preview objects for slots
                buildMenuSlot.CreatePreviewObject();
            }
            
            // Move to next column, or to the next row if we
            // reach max column count.
            column++;
            if ( column >= maximumNumberOfColumns )
            {
                row++;
                column = 0;
            }

            slotPositionX = column * horzontalButtonSpacing + origin.x;
            slotPositionY = -1 * row * verticalButtonSpacing + origin.y;
            i++;
        }
    }

    void DestroyChildren()
    {
        GameObject[] allChildren = new GameObject [ transform.childCount ] ;
        
        int i = 0;
        foreach ( Transform child in transform )
        {
            allChildren [ i ] = child.gameObject;
            i++;
        }
        
        foreach (GameObject child in allChildren)
        {
            DestroyImmediate(child.gameObject);
        }
    }
}
