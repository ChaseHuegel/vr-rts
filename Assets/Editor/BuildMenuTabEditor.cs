using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
