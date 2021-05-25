using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Fauna))]
public class FaunaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        Fauna fauna = (Fauna)target;

        EditorGUILayout.LabelField("Eat Chance:", ((fauna.eatActionChance) * 100).ToString());
        EditorGUILayout.LabelField("Look Around Chance:", ((fauna.lookAroundActionChance - fauna.eatActionChance) * 100).ToString());
        EditorGUILayout.LabelField("Idle Chance:", ((1.0f - fauna.lookAroundActionChance) * 100).ToString());
        
        EditorGUILayout.MinMaxSlider(ref fauna.eatActionChance, ref fauna.lookAroundActionChance, 0.0f, 1.0f);
        EditorUtility.SetDirty(target);
    }
}
