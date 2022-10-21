using System.Collections;
using System.Collections.Generic;
using Swordfish.Navigation;
using UnityEditor;
using UnityEngine;

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
            if (body) body.SyncToGrid();
        }
    }

    void Update()
    {
        if (autosnapOn)
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                Body body = go.GetComponent<Body>();
                if (body) body.SyncToGrid();
            }
        }
    }

}
