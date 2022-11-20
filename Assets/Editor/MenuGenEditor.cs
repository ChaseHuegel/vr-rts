using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//  [CustomEditor(typeof(MenuGen))]
//  public class MenuGenEditor : Editor
//  {
//     RTSUnitType unitType = RTSUnitType.None;
//     byte emptyButtonCount;

//     public override void OnInspectorGUI ()
//     {
//         if (GUILayout.Button("Add Empty Button"))
//             ((MenuGen)target).AddEmptyButton();

//         GUILayout.Space(10);

//         unitType = (RTSUnitType)EditorGUILayout.EnumPopup("Unit To Queue", unitType);

//         if (GUILayout.Button("Add Unit Queue Button"))
//             ((MenuGen)target).AddQueueButtonType(unitType);
        
//         GUILayout.Space(10);

//         DrawDefaultInspector();

//         GUILayout.Space(10);

//         if(GUILayout.Button("Generate Queue Menu"))
//             ((MenuGen)target).GenerateQueueMenu(true);

//         GUILayout.Space(10);

//         // if (GUILayout.Button("Generate Button Menu"))
//         //     ((MenuGen)target).GenerateButtonMenu();
        

        
//     }
// }
