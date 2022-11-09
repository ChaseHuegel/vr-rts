using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using Valve.VR.InteractionSystem;
using TMPro;

[Serializable]
public class BuildMenuTab : MonoBehaviour
{
    
    public float horzontalButtonSpacing = 71.0f;
    public float verticalButtonSpacing = 96.0f;
    public int maximumNumberOfColumns = 3;
    public GameObject resourceCostPrefab;
    public Material slotEnabledMaterial;
    public Material slotDisabledMaterial;
    public TMPro.TMP_FontAsset titleFont;
    public TechBase[] ButtonsNew;

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
        float rows = Mathf.Ceil((float)ButtonsNew.Length / (float)maximumNumberOfColumns);
        origin.y = (verticalButtonSpacing * -0.5f);
        origin.y += (verticalButtonSpacing * rows) * 0.5f;

        int i = 0;

        int row = 0;
        int column = 0;
        float slotPositionX = origin.x;
        float slotPositionY = origin.y;
        
        DestroyChildren();

        foreach (TechBase tech in ButtonsNew)
        {
            if (tech is BuildingData || tech is WallData)
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
                sphereCollider.radius = 0.045f;
                sphereCollider.center.Set(0.0f, 0.02f, 0.0f);

                // Instantiate the resource cost gameobject
                GameObject resourceCost = Instantiate(resourceCostPrefab, Vector3.zero, Quaternion.identity, slotObject.transform);
                resourceCost.transform.localPosition = new Vector3(0.0743f, -0.002f, 0.0f);
                resourceCost.transform.localRotation = Quaternion.identity;

                // Fetch and set the building type data from the database
                // TODO: Probably not needed if using scriptable objects
                buildMenuSlot.rtsTypeData = (BuildingData)tech;

                CreateSlotTitle(buildMenuSlot);

                // Popluate the resource cost prefab text objects
                BuildMenuResouceCost cost = resourceCost.GetComponent<BuildMenuResouceCost>();
                cost.woodText.text = buildMenuSlot.rtsTypeData.woodCost.ToString();
                cost.goldText.text = buildMenuSlot.rtsTypeData.goldCost.ToString();
                cost.grainText.text = buildMenuSlot.rtsTypeData.foodCost.ToString();
                cost.stoneText.text = buildMenuSlot.rtsTypeData.stoneCost.ToString();

                // Create/Instatiate preview objects for slots
                buildMenuSlot.CreatePreviewObject();
            }

            // Move to next column, or to the next row if we
            // reach max column count.
            column++;
            if (column >= maximumNumberOfColumns)
            {
                row++;
                column = 0;
            }

            slotPositionX = column * horzontalButtonSpacing + origin.x;
            slotPositionY = -1 * row * verticalButtonSpacing + origin.y;
            i++;
        }
    }

    private void CreateSlotTitle(BuildMenuSlot slot)
    {

        GameObject titleGameObject = new GameObject("_title");
        titleGameObject.transform.position = new Vector3(0.0513f, -0.0042f, 0.0f);
        titleGameObject.transform.SetParent(slot.transform, false);
        titleGameObject.transform.Rotate(90, 0, 90);

        TextMeshPro titleText = titleGameObject.AddComponent<TextMeshPro>();
        titleText.SetText(slot.rtsTypeData.title);
        titleText.fontStyle = FontStyles.Bold;
        titleText.fontSize = 0.10f;
        titleText.font = titleFont;
        titleText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        titleText.color = Color.white;
        titleText.raycastTarget = false;
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
