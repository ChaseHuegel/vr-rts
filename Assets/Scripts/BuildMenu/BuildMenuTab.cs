using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

 [CustomEditor(typeof(BuildMenuTab))]
 public class BuildMenuTabEditor : Editor
 {    
     public override void OnInspectorGUI ()
    {
        DrawDefaultInspector();

        if(GUILayout.Button("Generate Menu"))
        {    
            ((BuildMenuTab)target).Generate();
        }
    }
}

[Serializable]
public struct BuildMenuHoverButton
{    
    public GameObject visualModel;
    public int grainCost;
    public int woodCost;
    public int goldCost;
    public int oreCost;    
}

[ExecuteAlways]
[Serializable]
public class BuildMenuTab : MonoBehaviour
{   
    public GameObject resourceCostPrefab;
    public RTSBuildingType[] Buttons;
    void Start()
    {
        if (transform.childCount <= 0)
            Generate();

        
    }

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
            Vector3 position = new Vector3(x, y, 0);

            GameObject slot = Instantiate ( new GameObject ( "slot" + i ), position, Quaternion.identity, this.gameObject.transform );            
            slot.transform.localPosition = position;
            slot.layer = LayerMask.NameToLayer("UI");
            BuildMenuSlot buildMenuSlot = slot.AddComponent<BuildMenuSlot>();
            SphereCollider collider = slot.AddComponent<SphereCollider>();
            collider.radius = 40.0f;
            
            GameObject resourceCost = Instantiate(resourceCostPrefab, Vector3.zero, Quaternion.identity, slot.transform);
            resourceCost.transform.localPosition = Vector3.zero;
            
            BuildMenuResouceCost cost = resourceCost.GetComponent<BuildMenuResouceCost>();

            RTSBuildingTypeData typeData = GameMaster.Instance.FindBuildingData(rtsBuildingType);
            cost.woodText.text = typeData.woodCost.ToString();
            cost.goldText.text = typeData.goldCost.ToString();
            cost.grainText.text = typeData.grainCost.ToString();
            cost.oreText.text = typeData.oreCost.ToString();
            
            buildMenuSlot.rtsTypeData = typeData;

            if ( typeData.menuPreviewPrefab )
            {
                GameObject model = Instantiate( typeData.menuPreviewPrefab, slot.transform );
                model.transform.localPosition = Vector3.zero;
            }

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
