using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEditor;
using UnityEngine;

public class MapEditorWindow : EditorWindow
{
    public bool autosnapOn;

    [MenuItem("MapTools/MapTools Window")]
    static void Init()
    {
        MapEditorWindow window = (MapEditorWindow)GetWindow(typeof(MapEditorWindow));
        window.Show();
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

    void OnGUI()
    {
        autosnapOn = GUILayout.Toggle(autosnapOn, "Autosnap");

        if (GUILayout.Button("Snap Selection"))
            SnapSelection();

        if (GUILayout.Button("Building Data Editor"))
            OpenBuildingDataEditorWindow();

        if (GUILayout.Button("Tech Tree Editor"))
            OpenTechTreeWindow();

    }

    void OpenTechTreeWindow()
    {
        TechTreeEditor editor = EditorWindow.GetWindow<TechTreeEditor>();
    }

    void OpenBuildingDataEditorWindow()
    {
        BuildingDataCustomEditorWindow editor = EditorWindow.GetWindow<BuildingDataCustomEditorWindow>();
    }

    void SnapSelection()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Body body = go.GetComponent<Body>();
            if (body)
                HardSnapToGrid(body);
        }
    }

    public void HardSnapToGrid(Body body)
    {
        Vector3 pos = World.ToWorldSpace(body.transform.position);

        Coord2D gridPosition = body.GetPosition();

        gridPosition.x = Mathf.RoundToInt(pos.x);
        gridPosition.y = Mathf.RoundToInt(pos.z);

        body.transform.position = World.ToTransformSpace(new Vector3(gridPosition.x, body.transform.position.y, gridPosition.y));

        Vector3 modPos = body.transform.position;
        if (body.GetBoundingDimensions().x % 2 == 0)
            modPos.x = body.transform.position.x + World.GetUnit() * -0.5f;

        if (body.GetBoundingDimensions().y % 2 == 0)
            modPos.z = body.transform.position.z + World.GetUnit() * -0.5f;

        body.transform.position = modPos;
    }
}
