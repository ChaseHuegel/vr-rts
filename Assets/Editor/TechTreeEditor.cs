using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TechTreeEditor : EditorWindow
{
    // positioning
    Vector2 nodeSize = new Vector2(150, 80);
    // float minTreeHeight = 720f;
    // float minTreeWidth = 1000f;

    Vector2 nodeContentMargin = new Vector2(10.0f, 5.0f);
    Vector2 lineHeight = new Vector2(0f, 20f);
    float toggleButtonIndent = 102.0f;
    Vector2 nodeContentSize = new Vector2(40f, 20f);
    Vector2 nodeLabelSize = new Vector2(100f, 20f);

    // scrolling and moving
    Vector2 mouseSelectionOffset;
    Vector2 scrollPosition = Vector2.zero; // Move everything by scrollPosition
    Vector2 scrollStartPos;
    TechNode activeNode; // moved node stored here
    TechNode selectedNode; // selected node stored here
    public TechTree targetTree;

    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle inPointStyle;
    private GUIStyle outPointStyle;

    TechNode selectedInPointNode;
    TechNode selectedOutPointNode;

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
        nodeStyle.padding = new RectOffset(10, 0, 0, 0);

        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.fontStyle = FontStyle.BoldAndItalic;
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

        inPointStyle = new GUIStyle();
        inPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
        inPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
        inPointStyle.border = new RectOffset(4, 4, 12, 12);

        outPointStyle = new GUIStyle();
        outPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
        outPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
        outPointStyle.border = new RectOffset(4, 4, 12, 12);
    }

    public void OnGUI()
    {
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);

        targetTree = (TechTree)EditorGUILayout.ObjectField("Tech Tree", targetTree, typeof(TechTree), false);

        if (!targetTree)
            return;

        // Shows selected node tech and gives option to delete node
        EditorGUILayout.BeginHorizontal();
        if (selectedNode == null || selectedNode.tech == null)
        {
            EditorGUILayout.LabelField("selected tech: none");
        }
        else
        {
            EditorGUILayout.LabelField("seleceted tech: " + selectedNode.tech.name);
            if (GUILayout.Button("delete tech"))
            {
                targetTree.DeleteNode(selectedNode.tech);
                if (activeNode == selectedNode)
                    activeNode = null;

                selectedNode = null;
            }
        }
        GUILayout.EndHorizontal();

        // The techtree view
        //EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.MinHeight(720)); // the inspector height is set to 720

        if (targetTree.tree == null) 
            targetTree.tree = new List<TechNode>();
        
        DrawNodes(Event.current, nodeStyle, selectedNodeStyle);

        DrawConnectionLine(Event.current);

        ProcessEvents(Event.current);

        // EditorGUILayout.EndScrollView();

        // scrollPosition.x = GUILayout.HorizontalScrollbar(scrollPosition.x, 20.0f, 0.0f, minTreeWidth);
        // scrollPosition.y = GUI.VerticalScrollbar(new Rect(0, 0, 20, 720), scrollPosition.y, 20f, 0f, minTreeHeight);

        if (GUI.changed) Repaint();

        EditorUtility.SetDirty(targetTree); // Makes sure changes are persistent.
    }

    private void DrawNodes(Event currentEvent, GUIStyle nodeStyle, GUIStyle selectedNodeStyle)
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

    private void ProcessEvents(Event e)
    {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                    ClearConnectionSelection();

                if (e.button == 1)
                {
                    //ProcessContextMenu(e.mousePosition);
                }

                if (e.button == 2)
                    scrollStartPos = (e.mousePosition + scrollPosition); // Store the coordinate

                break;

            case EventType.MouseDrag:
                if (e.button == 0)
                { }

                if (e.button == 2)
                {
                    scrollPosition -= e.delta;
                    GUI.changed = true; // Repaint the GUI                
                }

                if (activeNode != null)
                {
                    activeNode.UIposition = e.mousePosition + mouseSelectionOffset;
                    GUI.changed = true;
                }

                break;

            case EventType.DragUpdated: // Import new tech
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                break;

            case EventType.DragPerform:
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    if (DragAndDrop.objectReferences[i] is TechBase)
                        targetTree.AddNode(DragAndDrop.objectReferences[i] as TechBase, e.mousePosition + scrollPosition);
                }
                break;

            case EventType.MouseUp:
                activeNode = null;
                break;
        }
    }

/* 
    public Action<Node> OnRemoveNode;

    private void ProcessContextMenu()
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Remove node"), false, OnClickRemoveNode);
        genericMenu.ShowAsContext();
    }

    private void OnClickRemoveNode()
    {
        if (OnRemoveNode != null)
        {
            OnRemoveNode(this);
        }
    }
 */
 
    private void DrawConnectionLine(Event e)
    {
        if (selectedInPointNode != null && selectedOutPointNode == null)
        {
            Rect nodeRect = new Rect(selectedInPointNode.UIposition - scrollPosition, nodeSize);
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
            Rect nodeRect = new Rect(selectedOutPointNode.UIposition - scrollPosition, nodeSize);
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
                        activeNode = targetTree.tree[nodeIdx];
                        mouseSelectionOffset = activeNode.UIposition - e.mousePosition; // offset from the corner of the node to mouse position
                    }
                }

                if (e.button == 1)  // If right mouse button is pressed
                {
                    if (nodeRect.Contains(e.mousePosition))
                    {
                        // Set selectedNode
                        selectedNode = targetTree.tree[nodeIdx];
                        GUI.changed = true; // Repaint to see the style change momentarily        
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
                Rect inNodeRect = new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition, nodeSize);
                Rect inPointRect = GetInPointRect(ref inNodeRect);

                Rect outNodeRect = new Rect(targetTree.tree[reqIdx].UIposition - scrollPosition, nodeSize);
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
        Rect nodeRect = new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition, nodeSize);

        Rect inPointRect = GetInPointRect(ref nodeRect);
        if (GUI.Button(inPointRect, "", inPointStyle))
            OnClickInPoint(targetTree.tree[nodeIdx]);

        Rect outPointRect = GetOutPointRect(ref nodeRect);
        if (GUI.Button(outPointRect, "", outPointStyle))
            OnClickOutPoint(targetTree.tree[nodeIdx]);

        GUI.Box(nodeRect, "", (selectedNode == targetTree.tree[nodeIdx] ? selectedNodeStyle : nodeStyle));

        Rect rowRect = new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition, nodeLabelSize);
        rowRect.x += nodeContentMargin.x;
        rowRect.y += lineHeight.y * 0.25f;

        GUIStyle style = new GUIStyle();
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        GUI.Label(rowRect, targetTree.tree[nodeIdx].tech.name, style);

        rowRect.y += lineHeight.y;
        
        GUI.Label(rowRect, "Unlocked");

        Rect toggleRect = new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition + lineHeight, nodeContentSize);
        toggleRect.x += toggleButtonIndent;
        toggleRect.y = rowRect.y;

        targetTree.tree[nodeIdx].unlocked = EditorGUI.Toggle(toggleRect, targetTree.tree[nodeIdx].unlocked);
        
        rowRect.y += lineHeight.y;
        GUI.Label(rowRect, "Researched");

        toggleRect.y = rowRect.y;
        targetTree.tree[nodeIdx].researched = EditorGUI.Toggle(toggleRect, targetTree.tree[nodeIdx].researched);

        return nodeRect;
    }

    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        Vector3 newOffset = new Vector3(-scrollPosition.x % gridSpacing, -scrollPosition.y % gridSpacing, 0);

        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
        }

        for (int j = 0; j < heightDivs; j++)
        {
            Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }
}
