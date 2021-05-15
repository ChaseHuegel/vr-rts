using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

 [CustomEditor(typeof(BuildingSpawnQueue))]
 public class BuildingSpawnQueueEditor : Editor
 {
     public override void OnInspectorGUI ()
    {
        DrawDefaultInspector();

        if(GUILayout.Button("Generate Menu"))
        {
            ((BuildingSpawnQueue)target).Generate();
        }
    }
}
