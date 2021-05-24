using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Swordfish.Navigation;
using UnityEditor;

 [ExecuteInEditMode]
public class MapTools : MonoBehaviour
{
    public bool autosnapOn;
    
    [InspectorButton("SnapSelection")]
    public bool snapSelection;

    void SnapSelection()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Body body = go.GetComponent<Body>();
            if (body)
                HardSnapToGrid(body);
        }
    }
  
    void Update()
    {
        if (autosnapOn)
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                Body body = go.GetComponent<Body>();
                if (body)
                    HardSnapToGrid(body);
            }
        }
    }

    public void HardSnapToGrid(Body body)
    {
        Vector3 pos = World.ToWorldSpace(body.transform.position);

        body.gridPosition.x = Mathf.RoundToInt(pos.x);
        body.gridPosition.y = Mathf.RoundToInt(pos.z);

        body.transform.position = World.ToTransformSpace(new Vector3(body.gridPosition.x, body.transform.position.y, body.gridPosition.y));
        
        Vector3 modPos = body.transform.position;
        if (body.boundingDimensions.x % 2 == 0)
            modPos.x = body.transform.position.x + World.GetUnit() * -0.5f;
        
        if (body.boundingDimensions.y % 2 == 0)
            modPos.z = body.transform.position.z + World.GetUnit() * -0.5f;

        body.transform.position = modPos;
    }

}
