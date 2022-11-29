using System.Collections;
using System.Collections.Generic;
using Swordfish;
using Swordfish.Navigation;
using UnityEditor;
using UnityEngine;

public class VRTSToolsWindow : EditorWindow
{
    //public bool autosnapOn;

    [MenuItem("VRTS/VRTS Tools")]
    static void Init()
    {
        VRTSToolsWindow window = (VRTSToolsWindow)GetWindow(typeof(VRTSToolsWindow));
        window.Show();
    }

    void Update()
    {
        // if (autosnapOn)
        // {
        //     foreach (GameObject gameObject in Selection.gameObjects)
        //         HardSnapToGrid(gameObject);
        // }
    }

    void OnGUI()
    {
        //autosnapOn = GUILayout.Toggle(autosnapOn, "Autosnap");

        if (GUILayout.Button("Snap Selection"))
            SnapSelection();

        if (GUILayout.Button("Building Data Editor"))
            OpenBuildingDataEditorWindow();

        if (GUILayout.Button("Tech Tree Editor"))
            OpenTechTreeWindow();

        EditorGUILayout.Space();

        if (GUILayout.Button("Set Unit SkinRenderer Targets"))
            SetSkinRendererTargets();

    }

    private void SetSkinRendererTargets()
    {
        GameObject gameObject = Selection.activeGameObject;

        UnitV2 unit = (UnitV2)Selection.activeGameObject.GetComponent<UnitV2>();
        if (unit == null)
            Debug.Log("No unit found.");

        Renderer[] skinnedMeshRenderers = Selection.activeGameObject.GetComponentsInChildren<Renderer>(false);        
        unit.SkinRendererTargets = new Renderer[skinnedMeshRenderers.Length];
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            unit.SkinRendererTargets[i] = skinnedMeshRenderers[i];
        }

        EditorUtility.SetDirty(unit);
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
            HardSnapToGrid(go);
        }
    }

    public void HardSnapToGrid(GameObject gameObject, bool verticalSnap = true)
    {
        Body body = gameObject.GetComponent<Body>();
        Vector3 pos = World.ToWorldSpace(gameObject.transform.position);
        Vector2 dimensions = new Vector2(1.0f, 1.0f);
        if (body != null)
            dimensions = body.GetBoundingDimensions();

        Coord2D gridPosition;

        gridPosition.x = Mathf.RoundToInt(pos.x);
        gridPosition.y = Mathf.RoundToInt(pos.z);

        gameObject.transform.position = World.ToTransformSpace(new Vector3(gridPosition.x, gameObject.transform.position.y, gridPosition.y));

        Vector3 modPos = gameObject.transform.position;
        if (dimensions.x % 2 == 0)
            modPos.x = gameObject.transform.position.x + World.GetUnit() * -0.5f;

        if (dimensions.y % 2 == 0)
            modPos.z = gameObject.transform.position.z + World.GetUnit() * -0.5f;

        gameObject.transform.position = modPos;

        float positionY = verticalSnap == true ? 0.0f : gameObject.transform.position.y;

        if (verticalSnap)
        {
            RaycastHit hit;
            Vector3 sourceLocation = gameObject.transform.position;
            sourceLocation.y += 10.0f;

            if (Physics.Raycast(sourceLocation, Vector3.down, out hit, 30.0f, LayerMask.GetMask("Terrain")))
                modPos.y = hit.point.y;
        }

        gameObject.transform.position = modPos;
    }
}
