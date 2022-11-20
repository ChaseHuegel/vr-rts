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
    Vector2 defaultNodeSize = new Vector2(210, 110);
    Vector2 unitNodeSize = new Vector2(160, 90);
    Vector2 buildingNodeSize = new Vector2(160, 90);
    Vector2 buildingNodeWithButtonsSize = new Vector2(160, 110);
    Vector2 epochNodeSize = new Vector2(190, 110);
    Vector2 researchNodeSize = new Vector2(180, 90);
    Vector2 upgradeNodeSize = new Vector2(180, 90);
    // scrolling and moving
    Vector2 mouseSelectionOffset;
    Vector2 scrollStartPos;
    TechNode draggedNode; // moved node stored here
    public TechNode selectedNode; // selected node stored here
    public TechTree targetTree;

    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle epochNodeStyle;
    private GUIStyle unitNodeStyle;
    private GUIStyle buildingNodeStyle;
    private GUIStyle researchNodeStyle;
    private GUIStyle upgradeNodeStyle;
    private GUIStyle inPointStyle;
    private GUIStyle outPointStyle;
    private GUIStyle nodeTitleStyle;
    private GUIStyle nodeTextStyle;

    TechNode selectedInPointNode;
    TechNode selectedOutPointNode;

    Vector2 startPos;
    Vector2 currentPos;
    bool drawingSelectionBox;

    [MenuItem("VRTS/Tech Tree Editor")]
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

        epochNodeStyle = new GUIStyle();
        epochNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node5.png") as Texture2D;
        epochNodeStyle.border = new RectOffset(12, 12, 12, 12);
        epochNodeStyle.padding = new RectOffset(10, 10, 0, 0);
        epochNodeStyle.alignment = TextAnchor.MiddleCenter;
        epochNodeStyle.normal.textColor = Color.black;

        unitNodeStyle = new GUIStyle();
        unitNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node3.png") as Texture2D;
        unitNodeStyle.border = new RectOffset(12, 12, 12, 12);
        unitNodeStyle.padding = new RectOffset(10, 10, 0, 0);
        unitNodeStyle.alignment = TextAnchor.MiddleCenter;

        buildingNodeStyle = new GUIStyle();
        buildingNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node2.png") as Texture2D;
        buildingNodeStyle.border = new RectOffset(12, 12, 12, 12);
        buildingNodeStyle.padding = new RectOffset(10, 10, 0, 0);
        buildingNodeStyle.alignment = TextAnchor.MiddleCenter;

        researchNodeStyle = new GUIStyle();
        researchNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node6.png") as Texture2D;
        researchNodeStyle.border = new RectOffset(12, 12, 12, 12);
        researchNodeStyle.padding = new RectOffset(10, 10, 0, 0);
        researchNodeStyle.alignment = TextAnchor.MiddleCenter;

        upgradeNodeStyle = new GUIStyle();
        upgradeNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node4.png") as Texture2D;
        upgradeNodeStyle.border = new RectOffset(12, 12, 12, 12);
        upgradeNodeStyle.padding = new RectOffset(10, 10, 0, 0);
        upgradeNodeStyle.alignment = TextAnchor.MiddleCenter;
        
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

        nodeTextStyle = new GUIStyle();
        nodeTextStyle.normal.textColor = Color.black;
        nodeTextStyle.margin = new RectOffset(0, 0, 4, 0);
        nodeTextStyle.alignment = TextAnchor.LowerCenter;
        nodeTextStyle.fontSize = 11;
    }

    private const float kZoomMin = 0.1f;
    private const float kZoomMax = 10.0f;
    private Rect _zoomArea = new Rect(0.0f, 75.0f, 600.0f, 300.0f - 100.0f);
    private float _zoom = 1.0f;
    private Vector2 _zoomCoordsOrigin = Vector2.zero;
    
    private Vector2 GetNodeSize(TechNode node)
    {
        if (node is EpochNode)
            return epochNodeSize;
        else if (node is BuildingNode)
        {
            BuildingData buildingData = (BuildingData)node.tech;

            if (buildingData.techQueueButtons.Count > 0)
                return buildingNodeWithButtonsSize;     
            else
                return buildingNodeSize;
        }
        else if (node is UnitNode)
            return unitNodeSize;
        else if (node is ResearchNode)
            return researchNodeSize;
        else if (node is UpgradeNode)
            return upgradeNodeSize;

        return defaultNodeSize;
    }

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
        DrawZoomArea();
        DrawNonZoomArea();

        //Selection.activeObject = selectedNode.tech;
        
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

        ProcessEvents(Event.current);

        DrawConnectionLine(Event.current);
        
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
            Rect nodeRect = DrawNode(targetTree.tree[nodeIdx]);

            // Draw Connections
            DrawNodeConnections(targetTree.tree[nodeIdx]);     

            ProcessNodeEvents(currentEvent, targetTree.tree[nodeIdx], nodeRect);
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
        genericMenu.AddItem(new GUIContent("Convert/To Unit Node"), false, OnClickConvertNode, NodeType.Unit);
        genericMenu.AddItem(new GUIContent("Convert/To Building Node"), false, OnClickConvertNode, NodeType.Building);
        genericMenu.AddItem(new GUIContent("Convert/To Epoch Node"), false, OnClickConvertNode, NodeType.Epoch);
        genericMenu.AddItem(new GUIContent("Convert/To Research Node"), false, OnClickConvertNode, NodeType.Research);
        genericMenu.AddItem(new GUIContent("Convert/To Upgrade Node"), false, OnClickConvertNode, NodeType.Upgrade);
        genericMenu.AddSeparator("");
        genericMenu.AddItem(new GUIContent("Reset Researched"), false, OnClickResetResearch);
        genericMenu.AddItem(new GUIContent("Reset IsBuilt"), false, OnClickResetIsBuilt);
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
    public void OnClickResetIsBuilt()
    {
        foreach (TechNode node in targetTree.tree)
        {
            if (node is BuildingNode)
                ((BuildingNode)node).isBuilt = false;
        }
        targetTree.RefreshNodes();
    }

    private enum NodeType
    {
        Unit,
        Building,
        Epoch,
        Research,
        Upgrade,
    }

    private void OnClickConvertNode(object num)
    {
        if (selectedNode == null)
            return;

        NodeType x = (NodeType)num;

        if (x == NodeType.Unit)
            ConvertToUnitNode();
        else if (x == NodeType.Building)
            ConvertToBuildingNode();
        else if (x == NodeType.Epoch)
            ConvertToEpochNode();
        else if (x == NodeType.Research)
            ConvertToResearchNode();
        else if (x == NodeType.Upgrade)
            ConvertToUpgradeNode();
    }

    private void ConvertToEpochNode()
    {
        TechBase tech = selectedNode.tech;
        EpochNode node = new EpochNode(tech, new List<TechBase>(), selectedNode.UIposition);
        targetTree.DeleteNode(tech);
        selectedNode = targetTree.AddNode(node, selectedNode.UIposition);
    }

    private void ConvertToUnitNode()
    {
        TechBase tech = selectedNode.tech;
        UnitNode node = new UnitNode(tech, new List<TechBase>(), selectedNode.UIposition);
        targetTree.DeleteNode(tech);
        selectedNode = targetTree.AddNode(node, selectedNode.UIposition);
    }

    private void ConvertToBuildingNode()
    {
        TechBase tech = selectedNode.tech;
        BuildingNode node = new BuildingNode(tech, new List<TechBase>(), selectedNode.UIposition);
        targetTree.DeleteNode(tech);
        selectedNode = targetTree.AddNode(node, selectedNode.UIposition);
    }

    private void ConvertToResearchNode()
    {
        TechBase tech = selectedNode.tech;
        ResearchNode node = new ResearchNode(tech, new List<TechBase>(), selectedNode.UIposition);
        targetTree.DeleteNode(tech);
        selectedNode = targetTree.AddNode(node, selectedNode.UIposition);
    }

    private void ConvertToUpgradeNode()
    {
        TechBase tech = selectedNode.tech;
        UpgradeNode node = new UpgradeNode(tech, new List<TechBase>(), selectedNode.UIposition);
        targetTree.DeleteNode(tech);
        selectedNode = targetTree.AddNode(node, selectedNode.UIposition);
    }

    private void DrawConnectionLine(Event e)
    {
        if (selectedInPointNode != null && selectedOutPointNode == null)
        {
            Rect nodeRect = new Rect(selectedInPointNode.UIposition + _zoomCoordsOrigin, GetNodeSize(selectedInPointNode));
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
            Rect nodeRect = new Rect(selectedOutPointNode.UIposition + _zoomCoordsOrigin, GetNodeSize(selectedOutPointNode));
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

    private bool snapEnabled;

    private void ProcessEvents(Event e)
    {
        if (drawingSelectionBox)
            EditorGUI.DrawRect(new Rect(startPos.x, startPos.y, currentPos.x - startPos.x, currentPos.y - startPos.y), new Color(0.1882353f, 0.3137255f, 0.5490196f, 0.5f));

        switch (e.type)
        {
            case EventType.KeyUp:
                if (e.keyCode == KeyCode.F9)
                    snapEnabled = !snapEnabled;
                break;

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

                        if (snapEnabled)
                        {
                            float x = Snapping.Snap(draggedNode.UIposition.x, 10.0f);
                            float y = Snapping.Snap(draggedNode.UIposition.y, 10.0f);
                            draggedNode.UIposition = new Vector2(x, y);
                        }

                        if (Event.current.modifiers == EventModifiers.Control)
                        {
                            float x = Snapping.Snap(draggedNode.UIposition.x, 10.0f);
                            float y = Snapping.Snap(draggedNode.UIposition.y, 10.0f);
                            draggedNode.UIposition = new Vector2(x, y);
                        }
                        
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
                    {
                        AddDraggedInNodeType(DragAndDrop.objectReferences[i] as TechBase, e.mousePosition - _zoomCoordsOrigin);                        
                    }
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

    private void ProcessNodeEvents(Event e, TechNode techNode, Rect nodeRect)
    {
        switch (e.type)
        { 
            case EventType.MouseDown:
                if (e.button == 0)  // If left mouse button is pressed
                {                    
                    // Set activeNode
                    if (nodeRect.Contains(e.mousePosition))
                    {
                        if (e.clickCount == 2)
                            OnNodeDoubleClick(techNode);

                        selectedNode = techNode;
                        draggedNode = techNode;                        
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
                        selectedNode = techNode;
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
                        selectedNode = techNode;                         
                        drawingSelectionBox = false;
                        GUI.changed = true; // Repaint to see the style change momentarily   
                        e.Use();
                    }
                }
                break;
        }
    }

    private void OnNodeDoubleClick(TechNode node)
    {
        Selection.activeObject = node.tech;
    }

    private void AddDraggedInNodeType(TechBase techBase, Vector2 position)
    {
        if (techBase is UnitData)
        {
            UnitNode node = new UnitNode(techBase, new List<TechBase>(), position);
            targetTree.AddNode(node, position);
        }
        else if (techBase is BuildingData)
        {
            BuildingNode node = new BuildingNode(techBase, new List<TechBase>(), position);
            targetTree.AddNode(node, position);
        }
        else if (techBase is EpochUpgrade)
        {
            EpochNode node = new EpochNode(techBase, new List<TechBase>(), position);
            node.requiredBuildingCount = ((EpochUpgrade)techBase).requiredBuildingCount;
            targetTree.AddNode(node, position);
        }
        else if (techBase is TechResearcher)
        {
            ResearchNode node = new ResearchNode(techBase, new List<TechBase>(), position);
            targetTree.AddNode(node, position);
        }
        else if (techBase is StatUpgrade)
        {
            UpgradeNode node = new UpgradeNode(techBase, new List<TechBase>(), position);
            targetTree.AddNode(node, position);
        }
        else
            targetTree.CreateNode(techBase, position);
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

    private void DrawNodeConnections(TechNode techNode)
    {
        foreach (TechBase req in techNode.techRequirements.ToArray())
        {
            int reqIdx = targetTree.FindTechIndex(req);
            if (reqIdx != -1)
            {
                Rect inNodeRect = new Rect(techNode.UIposition + _zoomCoordsOrigin, GetNodeSize(techNode));
                Rect inPointRect = GetInPointRect(ref inNodeRect);

                Rect outNodeRect = new Rect(targetTree.tree[reqIdx].UIposition + _zoomCoordsOrigin, GetNodeSize(targetTree.tree[reqIdx]));
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
                    OnClickRemoveConnection(techNode, targetTree.tree[reqIdx]);


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
        Rect inPointRect = new Rect(0, 0, 12f, 24f);
        inPointRect.y = nodeRect.y + (nodeRect.height * 0.5f) - inPointRect.height * 0.5f;
        inPointRect.x = nodeRect.x - inPointRect.width + 8f;
        return inPointRect;
    }

    private static Rect GetOutPointRect(ref Rect nodeRect)
    {
        Rect outPointRect = new Rect(0, 0, 12f, 24f);
        outPointRect.y = nodeRect.y + (nodeRect.height * 0.5f) - outPointRect.height * 0.5f;
        outPointRect.x = nodeRect.x + nodeRect.width - 8f;
        return outPointRect;
    }


    private Rect DrawNode(TechNode node)
    {
        GUIStyle areaStyle = nodeStyle;

        if (node is EpochNode)
            areaStyle = epochNodeStyle;
        else if (node is BuildingNode)        
            areaStyle = buildingNodeStyle;           
        else if (node is UnitNode)
            areaStyle = unitNodeStyle;
        else if (node is ResearchNode)
            areaStyle = researchNodeStyle;
        else if (node is UpgradeNode)
            areaStyle = upgradeNodeStyle;

        Rect nodeRect = new Rect(node.UIposition + _zoomCoordsOrigin, GetNodeSize(node));

        Rect inPointRect = GetInPointRect(ref nodeRect);
        if (GUI.Button(inPointRect, "", inPointStyle))
            OnClickInPoint(node);

        Rect outPointRect = GetOutPointRect(ref nodeRect);
        if (GUI.Button(outPointRect, "", outPointStyle))
            OnClickOutPoint(node);        

        if (selectedNode == node)
            GUI.Box(nodeRect, "", selectedNodeStyle);        

        GUILayout.BeginArea(nodeRect, areaStyle);        

        GUILayout.Label(node.tech.name, nodeTitleStyle);

        EditorGUILayout.BeginHorizontal();

        if (node.tech.worldQueueImage != null)
            GUILayout.Label(node.tech.worldQueueImage.texture, GUILayout.Width(52.0f), GUILayout.Height(52.0f));

        EditorGUILayout.BeginVertical();

        if (node is BuildingNode)
        {
            BuildingNode buildingNode = (BuildingNode)node;
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Is Built", nodeTextStyle);
            GUILayout.FlexibleSpace();
            if (buildingNode.isBuilt = EditorGUILayout.Toggle("", buildingNode.isBuilt))
                GUI.changed = true;

            EditorGUILayout.EndHorizontal();            
        }
        
        // EditorGUILayout.BeginHorizontal();
        // GUILayout.Label("Requires Research");
        // GUILayout.FlexibleSpace();
        // if (node.requiresResearch = EditorGUILayout.Toggle("", node.requiresResearch))
        //     GUI.changed = true;

        //EditorGUILayout.EndHorizontal();

        if (node is EpochNode || node is ResearchNode || node is UpgradeNode)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Researched", nodeTextStyle);
            GUILayout.FlexibleSpace();
            if (node.researched = EditorGUILayout.Toggle("", node.researched))
                GUI.changed = true;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Unlocked", nodeTextStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.Toggle("", node.unlocked);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Enabled", nodeTextStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.Toggle("", node.enabled);
        EditorGUILayout.EndHorizontal();

        if (node is EpochNode)
        {
            EpochNode epochNode = (EpochNode)node;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Req. Bldg #", nodeTextStyle);
            //EpochUpgrade epochUpgrade = (EpochUpgrade)epochNode.tech;
            //epochUpgrade.requiredBuildingCount = EditorGUILayout.IntField(epochUpgrade.requiredBuildingCount, GUILayout.MaxWidth(30.0f));
            epochNode.requiredBuildingCount = EditorGUILayout.IntField(epochNode.requiredBuildingCount, GUILayout.MaxWidth(30.0f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        if (node is BuildingNode)
        {
            EditorGUILayout.BeginHorizontal();
            BuildingNode buildingNode = (BuildingNode)node;
            BuildingData buildingData = (BuildingData)buildingNode.tech;

            GUIStyle queueButtonStyle = new GUIStyle();
            queueButtonStyle.margin = new RectOffset(1, 1, 1, 1);

            foreach (TechBase tech in buildingData.techQueueButtons)
            {
                if (tech.worldQueueImage != null)
                {
                    if (GUILayout.Button(new GUIContent(tech.worldQueueImage.texture, tech.title), queueButtonStyle, GUILayout.Width(24.0f), GUILayout.Height(24.0f)))
                    {
                        Selection.activeObject = tech;
                    }

                    // if (GUILayout.Button(tech.worldQueueImage.texture, queueButtonStyle, GUILayout.Width(24.0f), GUILayout.Height(24.0f)))
                    
                    // {
                    //     EditorGUILayout.Popup(new GUIContent("movementStepToMoveTo", "YOUR TOOLTIP HERE"));
                    //     if (Event.current.button == 0)
                    //     {
                    //         Selection.activeObject = tech;
                    //     }
                    //     // if (Event.current.button == 1)
                    //     //     ProcessContextMenu(tech);
                    // }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndArea();

        return nodeRect;
    }
}
