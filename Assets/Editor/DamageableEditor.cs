using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Swordfish;

[CustomEditor(typeof(Damageable))]
 public class DamageableEditor : Editor
 {
    public override void OnInspectorGUI ()
    {
        DrawDefaultInspector();

        if(GUILayout.Button("Fetch Stats"))
        {
            FetchStats();
        }
    }

    private void FetchStats()
    {
        Damageable damageable = (Damageable)target;
        Structure structure = damageable.gameObject.GetComponent<Structure>();
        BuildingData data = GameMaster.Instance.buildingDatabase.Get(structure.rtsBuildingType);

        ((Damageable)target).GetAttribute(Attributes.HEALTH).SetMax(data.hitPoints);
    }
}