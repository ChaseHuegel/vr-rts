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
    public Vector3 slotIconPosition;
    public Vector3 slotIconRotation;
    public Vector3 slotIconScale = Vector3.one;
    public Vector3 slotLockPosition;

    public TechBase[] ButtonsNew;

    public BuildMenuSlot[] Slots { get { return slots; } }
    private BuildMenuSlot[] slots;

    void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (slots == null)
            slots = GetComponentsInChildren<BuildMenuSlot>(true);

        foreach(BuildMenuSlot slot in slots)
            slot.Initialize();
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
                GameObject slot = new GameObject("_slot_" + i);
                slot.transform.SetParent(this.gameObject.transform);

                slot.transform.localPosition = new Vector3(slotPositionX, slotPositionY, origin.z);
                slot.transform.localRotation = Quaternion.Euler(0, 90, -90);
                slot.AddComponent<Interactable>();
                slot.GetComponent<Interactable>().highlightOnHover = false;

                // Set layer
                slot.layer = LayerMask.NameToLayer("UI");

                // Lock
                GameObject lockObject = Instantiate(PlayerManager.Instance.GetSlotLockPrefab(tech), Vector3.zero, Quaternion.identity, slot.transform);

                lockObject.name = "_lock";
                lockObject.transform.localPosition = slotLockPosition;
                lockObject.SetActive(false);

                // Icon
                GameObject iconObject = new GameObject("_icon");
                iconObject.transform.SetParent(slot.transform, false);
                iconObject.transform.localPosition = slotIconPosition;
                iconObject.transform.localScale = slotIconScale;
                iconObject.transform.localEulerAngles = slotIconRotation;
                iconObject.SetActive(false);

                SpriteRenderer spriteRenderer = iconObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = Sprite.Create(tech.worldQueueTexture, new Rect(0f, 0f, tech.worldQueueTexture.width, tech.worldQueueTexture.height), new Vector2(0.5f, 0.5f), 100.0f, 1, SpriteMeshType.Tight, Vector4.zero, true);

                // Collider - Must be added before BuildMenuSlot
                SphereCollider sphereCollider = slot.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.045f;
                sphereCollider.center.Set(0.0f, 0.02f, 0.0f);

                // BuildMenuSlot component
                BuildMenuSlot buildMenuSlotComponent = slot.AddComponent<BuildMenuSlot>();
                buildMenuSlotComponent.grabCollider = sphereCollider;

                // Resource cost gameobject
                GameObject resourceCostObject = Instantiate(resourceCostPrefab, Vector3.zero, Quaternion.identity, slot.transform);
                resourceCostObject.name = "_resource_cost";
                resourceCostObject.transform.localPosition = new Vector3(0.0743f, -0.002f, 0.0f);
                resourceCostObject.transform.localRotation = Quaternion.identity;

                // Fetch and set the building type data from the database
                buildMenuSlotComponent.rtsTypeData = (BuildingData)tech;
                buildMenuSlotComponent.lockObject = lockObject;
                buildMenuSlotComponent.iconObject = iconObject;

                CreateSlotTitle(buildMenuSlotComponent);

                // Popluate the resource cost prefab text objects
                BuildMenuResouceCost cost = resourceCostObject.GetComponent<BuildMenuResouceCost>();
                cost.woodText.text = buildMenuSlotComponent.rtsTypeData.woodCost.ToString();
                cost.goldText.text = buildMenuSlotComponent.rtsTypeData.goldCost.ToString();
                cost.grainText.text = buildMenuSlotComponent.rtsTypeData.foodCost.ToString();
                cost.stoneText.text = buildMenuSlotComponent.rtsTypeData.stoneCost.ToString();

                // Create/Instatiate preview objects for slots
                buildMenuSlotComponent.CreatePreviewObjects();                           
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

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
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
        titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0.02f);
        titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0.1f);
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
#if UNITY_EDITOR
            DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }
}
