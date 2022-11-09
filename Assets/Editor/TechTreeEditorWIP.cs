using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class TechTreeEditorWIP : EditorWindow
{
    // positioning
    Vector2 nodeSize = new Vector2(100f, 70f);
    float minTreeHeight = 720f;
    float minTreeWidth = 1000f;
    Vector2 incomingEdgeVec = new Vector2(100f, 10f);
    Vector2 outgoingEdgeVec = new Vector2(-12f, 10f);

    Vector2 upArrowVec = new Vector2(-10f, -10f);
    Vector2 downArrowVec = new Vector2(-10f, 10f);
    Vector2 nextLineVec = new Vector2(0f, 20f);
    Vector2 indentVec = new Vector2(102f, 0f);
    Vector2 nodeContentSize = new Vector2(40f, 20f);
    Vector2 nodeLabelSize = new Vector2(100f, 20f);

    // scrolling and moving
    Vector2 mouseSelectionOffset;
    Vector2 scrollPosition = Vector2.zero; // Move everything by scrollPosition
    Vector2 scrollStartPos;
    TechNode activeNode; // moved node stored here
    TechNode selectedNode; // selected node stored here

    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle inPointStyle;
    private GUIStyle outPointStyle;

    private Vector2 offset;
    private Vector2 drag;


    public TechTree targetTree;

    private void OnEnable()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);

        selectedNodeStyle = new GUIStyle();
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

    [MenuItem("Window/Tech Tree Editor WIP")]
    private static void OpenWindow()
    {
        TechTreeEditorWIP window = GetWindow<TechTreeEditorWIP>();
        window.titleContent = new GUIContent("Tech Tree Editor");
    }

    public void OnGUI()
    {
        targetTree = (TechTree)EditorGUILayout.ObjectField("Tech Tree", targetTree, typeof(TechTree), false);

        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);

        if (!targetTree) return;

        // Mouse events
        Event currentEvent = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        EventType UIEvent = currentEvent.GetTypeForControl(controlID);

        // node styles
        // GUIStyle nodeStyle = new GUIStyle(EditorStyles.helpBox);
        // GUIStyle selectedNodeStyle = new GUIStyle(EditorStyles.helpBox);
        // selectedNodeStyle.fontStyle = FontStyle.BoldAndItalic;

        // float topScrollPadding = 80.0f;
        // float bottomScrollPadding = 5.0f;
        // bottomScrollPadding += topScrollPadding;
        // float leftScrollPadding = 0.0f;
        // float rightScrollPadding = 5.0f;
        // rightScrollPadding += leftScrollPadding;

        // Rect paddedRect = new Rect(leftScrollPadding, topScrollPadding, position.width - rightScrollPadding, position.height - bottomScrollPadding);
        // scrollPosition = GUI.BeginScrollView(paddedRect, scrollPosition, new Rect(0, 0, 1920.0f, 1080.0f));

        if (targetTree.tree == null) targetTree.tree = new List<TechNode>();

        // Draw nodes and process events
        for (int nodeIdx = 0; nodeIdx < targetTree.tree.Count; nodeIdx++)
        {
            if (targetTree.tree[nodeIdx].tech == null)
            {
                targetTree.DeleteNode(null);
                continue;
            }

            // Draw node
            Rect nodeRect = DrawNode(nodeStyle, selectedNodeStyle, nodeIdx);

            // Draw Connections
            DrawConnections(nodeIdx);

            // Node events
            ProcessNodeEvents(currentEvent, UIEvent, nodeIdx, nodeRect);
        }

        ProcessMouseEvents(currentEvent);

        //GUI.EndScrollView();
        //Repaint();

        // Shows selecetd node tech and gives option to delete node
        GUILayout.BeginHorizontal();
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

        EditorUtility.SetDirty(targetTree); // Makes sure changes are persistent.

        if (GUI.changed) Repaint();
    }

    private void ProcessMouseEvents(Event e)
    {
        // Scroll in the Techtree view
        // if (e.button == 2) // If the middle mouse button is pressed, held, or released
        // {
        //     if (e.type == EventType.MouseDown) // If the mouse button is down
        //         scrollStartPos = (e.mousePosition + scrollPosition); // Store the coordinate
        //     else if (e.type == EventType.MouseDrag) // If the mouse button is held
        //     {
        //         scrollPosition = -(e.mousePosition - scrollStartPos); // Recalculate scrollPosition. This moves everything
        //         Repaint(); // Repaint the GUI
        //     }

        // }

        // Draw guiding connection when right mouse button is held
        if (selectedNode != null && e.button == 1) // If rightmouse button is used and selection is not empty.
        {
            // Draw connection guide between selected node and the mouse position.
            Handles.DrawBezier(e.mousePosition,
            selectedNode.UIposition - scrollPosition + incomingEdgeVec,
            e.mousePosition + Vector2.left * 100,
            selectedNode.UIposition - scrollPosition + incomingEdgeVec + Vector2.right * 100,
            Color.white, null, 1.5f);
            Repaint();
        }

        switch (e.type)
        {
            // Move nodes with the left mouse button
            case EventType.MouseUp: // If dropped
                {
                    if (e.button == 0)
                    {
                        activeNode = null;
                    }
                    break;
                }

            case EventType.MouseDown:
                {
                    break;
                }

            case EventType.MouseDrag:   // While dragged
                {
                    if (e.button == 0)
                    {
                        OnDrag(e.delta);
                    }
                    break;
                }
        }

        // Import new Tech
        if (e.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
        else if (e.type == EventType.DragPerform)
        {
            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                if (DragAndDrop.objectReferences[i] is TechBase)
                    targetTree.AddNode(DragAndDrop.objectReferences[i] as TechBase, e.mousePosition + scrollPosition);
            }
        }
    }

    private void OnDrag(Vector2 delta)
    {
        drag = delta;

        if (targetTree.tree != null)
        {
            for (int i = 0; i < targetTree.tree.Count; i++)
            {
                // Drag node
                targetTree.tree[i].UIposition += delta;
            }
        }

        GUI.changed = true;

        if (activeNode != null)
            activeNode.UIposition += delta; //e.mousePosition + mouseSelectionOffset;
    }

    private void ProcessNodeEvents(Event currentEvent, EventType UIEvent, int nodeIdx, Rect nodeRect)
    {
        if (nodeRect.Contains(currentEvent.mousePosition)) // If the cursor is on the node
        {
            if (UIEvent == EventType.MouseDown) // If a mouse button is pressed
            {
                // Set activeNode
                if (currentEvent.button == 0) // If left mouse button is pressed
                {
                    activeNode = targetTree.tree[nodeIdx];
                    mouseSelectionOffset = activeNode.UIposition - currentEvent.mousePosition; // offset from the corner of the node to mouse position
                }
                // Set selectedNode
                else if (currentEvent.button == 1) // If right mouse button is pressed
                {
                    selectedNode = targetTree.tree[nodeIdx];
                    GUI.changed = true;
                    //Repaint(); // Repaint to see the style change momentarily                        
                }
            }
            // Create/Destroy connections
            else if (UIEvent == EventType.MouseUp) // If the mouse button is released
            {
                // If right button is released and selectionNode is not empty
                if (currentEvent.button == 1 && selectedNode != null && selectedNode != targetTree.tree[nodeIdx])
                {
                    // Remove any connection between the selectedNode and hovered node if exists
                    if (targetTree.tree[nodeIdx].techRequirements.Contains(selectedNode.tech))
                        targetTree.tree[nodeIdx].techRequirements.Remove(selectedNode.tech);
                    else if (selectedNode.techRequirements.Contains(targetTree.tree[nodeIdx].tech))
                        selectedNode.techRequirements.Remove(targetTree.tree[nodeIdx].tech);
                    // If doesn't exist and they are connectible then create a connection
                    else if (targetTree.IsConnectible(targetTree.tree.IndexOf(selectedNode), nodeIdx))
                    {
                        targetTree.tree[nodeIdx].techRequirements.Add(selectedNode.tech);

                        // Creating connection may annul other requirement connections, so check all connections.
                        for (int k = 0; k < targetTree.tree.Count; k++)
                            targetTree.CorrectRequirementCascades(k);
                    }

                    GUI.changed = true;
                }
            }
        }
    }

    private void DrawConnections(int nodeIdx)
    {
        foreach (TechBase req in targetTree.tree[nodeIdx].techRequirements)
        {
            int reqIdx = targetTree.FindTechIndex(req);
            if (reqIdx != -1)
            {
                // Draw connecting curve
                Handles.DrawBezier(targetTree.tree[nodeIdx].UIposition - scrollPosition + outgoingEdgeVec,
                    targetTree.tree[reqIdx].UIposition - scrollPosition + incomingEdgeVec,
                    targetTree.tree[nodeIdx].UIposition - scrollPosition + outgoingEdgeVec + Vector2.left * 100,
                    targetTree.tree[reqIdx].UIposition - scrollPosition + incomingEdgeVec + Vector2.right * 100,
                    Color.white, null, 3.0f);

                // Draw arrow
                Handles.DrawLine(targetTree.tree[nodeIdx].UIposition - scrollPosition + outgoingEdgeVec, targetTree.tree[nodeIdx].UIposition - scrollPosition + outgoingEdgeVec + upArrowVec);
                Handles.DrawLine(targetTree.tree[nodeIdx].UIposition - scrollPosition + outgoingEdgeVec, targetTree.tree[nodeIdx].UIposition - scrollPosition + outgoingEdgeVec + downArrowVec);
            }
            else
                Debug.LogWarning("missing tech " + req.name);
        }
    }

    private Rect DrawNode(GUIStyle nodeStyle, GUIStyle selectedStyle, int nodeIdx)
    {
        Rect nodeRect = new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition, nodeSize);
        GUI.Box(nodeRect, targetTree.tree[nodeIdx].tech.name, (selectedNode == targetTree.tree[nodeIdx] ? selectedStyle : nodeStyle));

        // Unlocked
        GUI.Label(new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition + nextLineVec, nodeLabelSize), "Unlocked");
        targetTree.tree[nodeIdx].unlocked = EditorGUI.Toggle(new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition + nextLineVec + indentVec, nodeContentSize), targetTree.tree[nodeIdx].unlocked);

        // Researched
        GUI.Label(new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition + nextLineVec * 2, nodeLabelSize), "Researched");
        targetTree.tree[nodeIdx].researched = EditorGUI.Toggle(new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition + nextLineVec * 2 + indentVec, nodeContentSize), targetTree.tree[nodeIdx].researched);

        return nodeRect;
    }

    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

        offset += drag * 0.5f;
        Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);

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
