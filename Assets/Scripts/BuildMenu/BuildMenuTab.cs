using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using Valve.VR.InteractionSystem;

[Serializable]
public struct BuildMenuHoverButton
{
    public GameObject visualModel;
    public int grainCost;
    public int woodCost;
    public int goldCost;
    public int stoneCost;
}

[Serializable]
public class BuildMenuTab : MonoBehaviour
{
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
        int i = 0;

        int maxColumns = 3;

        int xSpacing = 80;
        int ySpacing = 80;

        int originX = -80; // start
        int originY = 120; // start

        int row = 0;
        int column = 0;
        int x = originX;
        int y = originY;
        
        DestroyChildren();

        foreach ( RTSBuildingType rtsBuildingType in Buttons )
        {
            if (rtsBuildingType != RTSBuildingType.None)
            {

                // Create the button slot gameobject
                GameObject slot = Instantiate ( new GameObject ( "slot" + i ), this.gameObject.transform );

                // Iterate through slot positions
                slot.transform.localPosition = new Vector3(x, y, 0);

                slot.transform.Rotate(0, 90, -90);

                // Set layer
                slot.layer = LayerMask.NameToLayer("UI");

                // Add components needed
                BuildMenuSlot buildMenuSlot = slot.AddComponent<BuildMenuSlot>();
                

                SphereCollider sphereCollider = slot.AddComponent<SphereCollider>();
                sphereCollider.radius = 40.0f;

                // Instantiate the resource cost gameobject
                GameObject resourceCost = Instantiate(resourceCostPrefab, Vector3.zero, Quaternion.identity, slot.transform);
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
            if ( column >= maxColumns )
            {
                row++;
                column = 0;
            }

            x = column * xSpacing + originX;
            y = -1 * row * ySpacing + originY;
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

        foreach ( GameObject child in allChildren )
        {
            DestroyImmediate ( child.gameObject );
        }
    }
}
