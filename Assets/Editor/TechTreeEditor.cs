using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

class GetStringInputWindow : EditorWindow
{

    string fileName;

    void OnGUI()
    {
        fileName = EditorGUILayout.TextField("Name", fileName);

        if (GUILayout.Button("OK"))
        {
            OnClickOk();
            GUIUtility.ExitGUI();
        }
    }

    void OnClickOk()
    {
        fileName = fileName.Trim();

        if (string.IsNullOrEmpty(fileName))
        {
            EditorUtility.DisplayDialog("Name required", "Please specify a valid name.", "Close");
            return;
        }

        // You may also want to check for illegal characters :)

        // Save your prefab

        Close();
    }

}

public class TechTreeEditor : EditorWindow
{
    // positioning
    Vector2 nodeSize = new Vector2(220, 110);

    // scrolling and moving
    Vector2 mouseSelectionOffset;
    Vector2 scrollStartPos;
    TechNode draggedNode; // moved node stored here
    TechNode selectedNode; // selected node stored here
    public TechTree targetTree;

    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle inPointStyle;
    private GUIStyle outPointStyle;
    private GUIStyle nodeTitleStyle;

    TechNode selectedInPointNode;
    TechNode selectedOutPointNode;

    Vector2 startPos;
    Vector2 currentPos;
    bool drawingSelectionBox;

    [MenuItem("Window/Tech Tree Editor")]
    private static void OpenWindow()
    {
        TechTreeEditor window = GetWindow<TechTreeEditor>();
        window.titleContent = new GUIContent("Tech Tree Editor");
    }

    private void OnEnable()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);
        nodeStyle.padding = new RectOffset(10, 10, 0, 0);
        nodeStyle.alignment = TextAnchor.MiddleCenter;
        
        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.fontStyle = FontStyle.BoldAndItalic;
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);
        selectedNodeStyle.padding = new RectOffset(10, 10, 0, 0);
        selectedNodeStyle.alignment = TextAnchor.MiddleCenter;


        inPointStyle = new GUIStyle();
        inPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
        inPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
        inPointStyle.border = new RectOffset(4, 4, 12, 12);

        outPointStyle = new GUIStyle();
        outPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
        outPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
        outPointStyle.border = new RectOffset(4, 4, 12, 12);

        nodeTitleStyle = new GUIStyle();
        nodeTitleStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn.png") as Texture2D;
        nodeTitleStyle.border = new RectOffset(4, 4, 1, 1);
        nodeTitleStyle.fontStyle = FontStyle.Bold;
        nodeTitleStyle.normal.textColor = Color.white;
        nodeTitleStyle.alignment = TextAnchor.UpperCenter;        
    }

    private const float kZoomMin = 0.1f;
    private const float kZoomMax = 10.0f;
    private Rect _zoomArea = new Rect(0.0f, 75.0f, 600.0f, 300.0f - 100.0f);
    private float _zoom = 1.0f;
    private Vector2 _zoomCoordsOrigin = Vector2.zero;
    
    public void OnGUI()
    {
        Rect graphPosition = new Rect(0f, 0f, position.width, position.height);
        GraphBackground.DrawGraphBackground(graphPosition, graphPosition);
        
        _zoomArea = new Rect(0.0f, 0.0f, position.width, position.height);
        targetTree = (TechTree)EditorGUILayout.ObjectField("Tech Tree", targetTree, typeof(TechTree), false);        

        if (!targetTree)
            return;

        // The zoom area clipping is sometimes not fully confined to the passed in rectangle. At certain
        // zoom levels you will get a line of pixels rendered outside of the passed in area because of
        // floating point imprecision in the scaling. Therefore, it is recommended to draw the zoom
        // area first and then draw everything else so that there is no undesired overlap.
        DrawNonZoomArea();
        DrawZoomArea();

        if (GUI.changed)
        {
            targetTree.RefreshNodes();
            Repaint();
        }

        EditorUtility.SetDirty(targetTree); // Makes sure changes are persistent.
    }

    private void DrawNonZoomArea()
    {
        // Shows selected node tech and gives option to delete node
        EditorGUILayout.BeginHorizontal();
        if (selectedNode == null || selectedNode.tech == null)
        {
            EditorGUILayout.LabelField("selected tech: none");
        }
        else
        {
            EditorGUILayout.LabelField("seleceted tech: " + selectedNode.tech.name);
            // if (GUILayout.Button("delete tech"))
            // {
            //     RemoveNode();
            // }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawZoomArea()
    {
        // Within the zoom area all coordinates are relative to the top left corner of the zoom area
        // with the width and height being scaled versions of the original/unzoomed area's width and height.
        EditorZoomArea.Begin(_zoom, _zoomArea);

        if (targetTree.tree == null)
            targetTree.tree = new List<TechNode>();

        DrawNodes(Event.current);

        DrawConnectionLine(Event.current);

        ProcessEvents(Event.current);
        
        EditorZoomArea.End();
    }

    private void DrawNodes(Event currentEvent)
    {
        for (int nodeIdx = 0; nodeIdx < targetTree.tree.Count; nodeIdx++)
        {
            if (targetTree.tree[nodeIdx].tech == null)
            {
                targetTree.DeleteNode(null);
                continue;
            }    

            // Draw node
            Rect nodeRect = DrawNode( nodeIdx);

            // Draw Connections
            DrawNodeConnections(nodeIdx);     

            ProcessNodeEvents(currentEvent, nodeIdx, nodeRect);
        }
    }

    private void OnPan(Event e)
    {
        _zoomCoordsOrigin += e.delta;
        e.Use();        
        GUI.changed = true; // Repaint the GUI 
    }

    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("New Researcher Node"), false, OnClickNewResearcherNode);
        genericMenu.AddItem(new GUIContent("New Epoch Node"), false, OnClickNewEpochNode);
        genericMenu.AddItem(new GUIContent("New Tech Tree"), false, OnClickNewTechTree);
        genericMenu.AddSeparator("");
        genericMenu.AddItem(new GUIContent("Reset Researched"), false, OnClickResetResearch);
        genericMenu.AddSeparator("");
        genericMenu.AddItem(new GUIContent("Remove node"), false, RemoveNode);
        genericMenu.ShowAsContext();
    }

    /* public Action<Node> OnRemoveNode;
    private void RemoveNode()
    {
        if (OnRemoveNode != null)
        {
            OnRemoveNode(this);
        }
    } */

    private void RemoveNode()
    {
        if (selectedNode == null)
            return;

        targetTree.DeleteNode(selectedNode.tech);
        if (draggedNode == selectedNode)
            draggedNode = null;

        selectedNode = null;
    }

    public void OnClickResetResearch()
    {
        foreach (TechNode node in targetTree.tree)
        {
            node.researched = false;
        }
        targetTree.RefreshNodes();
    }

    public void OnClickNewResearcherNode()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save", "New Research Node", "", "Blah");
        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log(path);
        }

        // GetStringInputWindow inputWindow = new GetStringInputWindow();
        // inputWindow.Show();
    }

    public void OnClickNewEpochNode()
    {

    }

    public void OnClickNewTechTree()
    {

    }

 
    private void DrawConnectionLine(Event e)
    {
        if (selectedInPointNode != null && selectedOutPointNode == null)
        {
            Rect nodeRect = new Rect(selectedInPointNode.UIposition + _zoomCoordsOrigin, nodeSize);
            Rect inPointRect = GetInPointRect(ref nodeRect);

            Handles.DrawBezier(
                inPointRect.center,
                e.mousePosition,
                inPointRect.center + Vector2.left * 50f,
                e.mousePosition - Vector2.left * 50f,
                Color.white,
                null,
                2f
            );

            GUI.changed = true;
        }

        if (selectedOutPointNode != null && selectedInPointNode == null)
        {
            Rect nodeRect = new Rect(selectedOutPointNode.UIposition + _zoomCoordsOrigin, nodeSize);
            Rect outPointRect = GetOutPointRect(ref nodeRect);

            Handles.DrawBezier(
                outPointRect.center,
                e.mousePosition,
                outPointRect.center - Vector2.left * 50f,
                e.mousePosition + Vector2.left * 50f,
                Color.white,
                null,
                2f
            );

            GUI.changed = true;
        }
    }

    
    private void ProcessEvents(Event e)
    {
        if (drawingSelectionBox)
            EditorGUI.DrawRect(new Rect(startPos.x, startPos.y, currentPos.x - startPos.x, currentPos.y - startPos.y), new Color(0.1882353f, 0.3137255f, 0.5490196f, 0.5f));

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    ClearConnectionSelection();
                    selectedNode = null;
                    draggedNode = null;
                    GUI.changed = true; // Repaint to see the style change momentarily
                }

                else if (e.button == 1)
                { }
                break;

            case EventType.MouseDrag:                
                if (e.button == 0)
                {
                    currentPos = e.mousePosition;
                    if (!drawingSelectionBox && draggedNode == null)
                    {
                        drawingSelectionBox = true;
                        startPos = currentPos;
                    }

                    if (Event.current.modifiers == EventModifiers.Alt)
                        OnPan(Event.current);

                    else if (draggedNode != null)
                    {
                        draggedNode.UIposition = e.mousePosition + mouseSelectionOffset;
                        GUI.changed = true;
                    }
                }
                else if (e.button == 2)
                    OnPan(Event.current);

                break;

            // Allow adjusting the zoom with the mouse wheel. Use the mouse coordinates
            // as the zoom center. This is achieved by maintaining an origin that is 
            // used as offset when drawing any GUI elements in the zoom area.
            // BROKEN
            case EventType.ScrollWheel:
                {
                    Vector2 delta = Event.current.delta;
                    Vector2 zoomCoordsMousePos = ((Event.current.mousePosition - _zoomArea.TopLeft()) / _zoom) + _zoomCoordsOrigin;
                    float zoomDelta = -delta.y / 150.0f;
                    float oldZoom = _zoom;
                    _zoom += zoomDelta;
                    _zoom = Mathf.Clamp(_zoom, kZoomMin, kZoomMax);
                    _zoomCoordsOrigin += (zoomCoordsMousePos - _zoomCoordsOrigin) - ((oldZoom / _zoom) * (zoomCoordsMousePos - _zoomCoordsOrigin));

                    Event.current.Use();
                }
                break;

            case EventType.DragUpdated: // Import new tech
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;                
                break;

            case EventType.DragPerform:
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    if (DragAndDrop.objectReferences[i] is TechBase)
                        targetTree.AddNode(DragAndDrop.objectReferences[i] as TechBase, e.mousePosition - _zoomCoordsOrigin);
                }                
                break;

            case EventType.MouseUp:
                if (e.button == 0)
                {
                    // selectedNode = null;
                    // GUI.changed = true; // Repaint to see the style change momentarily
                }
                else if (e.button == 1)
                {
                    ProcessContextMenu(e.mousePosition);
                }
                drawingSelectionBox = false;
                draggedNode = null;
                break;
        }
    }

    private void ProcessNodeEvents(Event e, int nodeIdx, Rect nodeRect)
    {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)  // If left mouse button is pressed
                {   
                    // Set activeNode
                    if (nodeRect.Contains(e.mousePosition))
                    {
                        selectedNode = targetTree.tree[nodeIdx];
                        draggedNode = targetTree.tree[nodeIdx];
                        mouseSelectionOffset = draggedNode.UIposition - e.mousePosition; // offset from the corner of the node to mouse position
                        GUI.changed = true; // Repaint to see the style change momentarily 
                        drawingSelectionBox = false;
                        e.Use();
                    }
                }

                if (e.button == 1)  // If right mouse button is pressed
                {
                    if (nodeRect.Contains(e.mousePosition))
                    {
                        // Set selectedNode
                        selectedNode = targetTree.tree[nodeIdx];
                        drawingSelectionBox = false;
                        GUI.changed = true; // Repaint to see the style change momentarily        
                    }
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0)
                {
                    if (nodeRect.Contains(e.mousePosition))
                    {
                        // Set selectedNode
                        selectedNode = targetTree.tree[nodeIdx];                         
                        drawingSelectionBox = false;
                        GUI.changed = true; // Repaint to see the style change momentarily   
                        e.Use();
                    }
                }
                break;
        }
    }

    private void OnClickInPoint(TechNode inPointNode)
    {
        selectedInPointNode = inPointNode;

        if (selectedOutPointNode != null)
        {
            if (selectedOutPointNode != selectedInPointNode)
            {
                CreateConnection();
                ClearConnectionSelection();
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }

    private void OnClickOutPoint(TechNode outPointNode)
    {
        selectedOutPointNode = outPointNode;

        if (selectedInPointNode != null)
        {
            if (selectedInPointNode != selectedOutPointNode)
            {
                CreateConnection();
                ClearConnectionSelection();
            }
            else
            {
                ClearConnectionSelection();
            }
        }
    }

    private void CreateConnection()
    {
        if (targetTree.IsConnectible(targetTree.tree.IndexOf(selectedInPointNode), targetTree.tree.IndexOf(selectedOutPointNode)))
        {
            selectedInPointNode.techRequirements.Add(selectedOutPointNode.tech);

            // Creating connection may annul other requirement connections, so check all connections.
            for (int k = 0; k < targetTree.tree.Count; k++)
                targetTree.CorrectRequirementCascades(k);

            ClearConnectionSelection();
        }
    }

    private void ClearConnectionSelection()
    {
        selectedInPointNode = null;
        selectedOutPointNode = null;
    }

    private void DrawNodeConnections(int nodeIdx)
    {
        foreach (TechBase req in targetTree.tree[nodeIdx].techRequirements.ToArray())
        {
            int reqIdx = targetTree.FindTechIndex(req);
            if (reqIdx != -1)
            {
                Rect inNodeRect = new Rect(targetTree.tree[nodeIdx].UIposition + _zoomCoordsOrigin, nodeSize);
                Rect inPointRect = GetInPointRect(ref inNodeRect);

                Rect outNodeRect = new Rect(targetTree.tree[reqIdx].UIposition + _zoomCoordsOrigin, nodeSize);
                Rect outPointRect = GetOutPointRect(ref outNodeRect);

                Handles.DrawBezier(
                    inPointRect.center,
                    outPointRect.center,
                    inPointRect.center + Vector2.left * 50f,
                    outPointRect.center - Vector2.left * 50f,
                    Color.white,
                    null,
                    2f
                );

                if (Handles.Button((inPointRect.center + outPointRect.center) * 0.5f, Quaternion.identity, 4, 8, Handles.RectangleHandleCap))
                    OnClickRemoveConnection(targetTree.tree[nodeIdx], targetTree.tree[reqIdx]);

            }
            else
                Debug.LogWarning("missing tech " + req.name);
        }
    }

    private void OnClickRemoveConnection(TechNode techNodeFirst, TechNode techNodeSecond)
    {
        if (techNodeFirst.techRequirements.Contains(techNodeSecond.tech))
            techNodeFirst.techRequirements.Remove(techNodeSecond.tech);

        else if (techNodeSecond.techRequirements.Contains(techNodeFirst.tech))
            techNodeSecond.techRequirements.Remove(techNodeFirst.tech);
    }

    private static Rect GetInPointRect(ref Rect nodeRect)
    {
        Rect inPointRect = new Rect(0, 0, 10f, 20f);
        inPointRect.y = nodeRect.y + (nodeRect.height * 0.5f) - inPointRect.height * 0.5f;
        inPointRect.x = nodeRect.x - inPointRect.width + 8f;
        return inPointRect;
    }

    private static Rect GetOutPointRect(ref Rect nodeRect)
    {
        Rect outPointRect = new Rect(0, 0, 10f, 20f);
        outPointRect.y = nodeRect.y + (nodeRect.height * 0.5f) - outPointRect.height * 0.5f;
        outPointRect.x = nodeRect.x + nodeRect.width - 8f;
        return outPointRect;
    }

    private Rect DrawNode(int nodeIdx)
    {
        Rect nodeRect = new Rect(targetTree.tree[nodeIdx].UIposition + _zoomCoordsOrigin, nodeSize);

        Rect inPointRect = GetInPointRect(ref nodeRect);
        if (GUI.Button(inPointRect, "", inPointStyle))
            OnClickInPoint(targetTree.tree[nodeIdx]);

        Rect outPointRect = GetOutPointRect(ref nodeRect);
        if (GUI.Button(outPointRect, "", outPointStyle))
            OnClickOutPoint(targetTree.tree[nodeIdx]);

        GUILayout.BeginArea(nodeRect, (selectedNode == targetTree.tree[nodeIdx] ? selectedNodeStyle : nodeStyle));

        GUILayout.Label(targetTree.tree[nodeIdx].tech.name, nodeTitleStyle);        

        EditorGUILayout.BeginHorizontal();

        if (targetTree.tree[nodeIdx].tech.worldQueueImage != null)
            GUILayout.Label(targetTree.tree[nodeIdx].tech.worldQueueImage.texture, GUILayout.Width(52.0f), GUILayout.Height(52.0f));

        EditorGUILayout.BeginVertical();        

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Requires Research");
        GUILayout.FlexibleSpace();
        if (targetTree.tree[nodeIdx].requiresResearch = EditorGUILayout.Toggle("", targetTree.tree[nodeIdx].requiresResearch))
            GUI.changed = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Researched");
        GUILayout.FlexibleSpace();
        if (targetTree.tree[nodeIdx].researched = EditorGUILayout.Toggle("", targetTree.tree[nodeIdx].researched))
            GUI.changed = true;
            
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Unlocked");
        GUILayout.FlexibleSpace();
        EditorGUILayout.Toggle("", targetTree.tree[nodeIdx].unlocked);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Enabled");
        GUILayout.FlexibleSpace();
        EditorGUILayout.Toggle("", targetTree.tree[nodeIdx].enabled);
        EditorGUILayout.EndHorizontal();

        // EditorGUILayout.BeginHorizontal();
        // GUILayout.Label("Required Tech#");
        // targetTree.tree[nodeIdx].requiredTechCount = EditorGUILayout.IntField(targetTree.tree[nodeIdx].requiredTechCount, GUILayout.MaxWidth(30.0f));
        // GUILayout.FlexibleSpace();
        // EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        GUILayout.EndArea();

        return nodeRect;
    }
}
