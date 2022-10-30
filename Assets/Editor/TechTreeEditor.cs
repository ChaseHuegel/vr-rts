using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Techtree))]
public class TechTreeEditor : Editor
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

    public override void OnInspectorGUI()
    {
        Techtree targetTree = (Techtree)target; // get the associated Techtree
        
        // Mouse events
        Event currentEvent = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        EventType UIEvent = currentEvent.GetTypeForControl(controlID);

        // node styles
        GUIStyle nodeStyle = new GUIStyle(EditorStyles.helpBox);
        GUIStyle selectedStyle = new GUIStyle(EditorStyles.helpBox);
        selectedStyle.fontStyle = FontStyle.BoldAndItalic;
        
        // The techtree view
        EditorGUILayout.BeginScrollView(Vector2.zero, GUILayout.MinHeight(720)); // the inspector height is set to 720

        if (targetTree.tree == null) targetTree.tree = new List<TechNode>();
        
        for (int nodeIdx = 0; nodeIdx < targetTree.tree.Count; nodeIdx++)
        {
            if (targetTree.tree[nodeIdx].tech == null)
            {
                targetTree.DeleteNode(null);
                continue;
            }
            
            // Draw node
            Rect nodeRect = new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition, nodeSize);
            EditorGUI.BeginFoldoutHeaderGroup(nodeRect, true, targetTree.tree[nodeIdx].tech.name, (selectedNode == targetTree.tree[nodeIdx] ? selectedStyle : nodeStyle));
            EditorGUI.LabelField(new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition + nextLineVec, nodeLabelSize), "Research Cost");
            targetTree.tree[nodeIdx].researchCost = EditorGUI.IntField(new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition + nextLineVec + indentVec, nodeContentSize), targetTree.tree[nodeIdx].researchCost);
            EditorGUI.LabelField(new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition + nextLineVec * 2, nodeLabelSize), "Invested");
            targetTree.tree[nodeIdx].researchInvested = EditorGUI.IntField(new Rect(targetTree.tree[nodeIdx].UIposition - scrollPosition + nextLineVec * 2 + indentVec, nodeContentSize), targetTree.tree[nodeIdx].researchInvested);
            EditorGUI.EndFoldoutHeaderGroup();

            // Draw Connections
            foreach (Tech req in targetTree.tree[nodeIdx].requirements)
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

            // Mouse events
            if (nodeRect.Contains(currentEvent.mousePosition)) // If the cursor is on the node
            {
                if (UIEvent == EventType.MouseDown) // If a mouse button is pressed
                {
                    // Set activeNode
                    if (currentEvent.button == 0) // If left mouse button is pressed
                    {
                        activeNode = targetTree.tree[nodeIdx];
                        mouseSelectionOffset = activeNode.UIposition - currentEvent.mousePosition; // offset from th ecorner of the node to mouse position
                    }
                    // Set selectedNode
                    else if (currentEvent.button == 1) // If right mouse button is pressed
                    {
                        selectedNode = targetTree.tree[nodeIdx];
                        Repaint(); // Repaint to see the style change momentarily                        
                    }
                }
                // Create/Destroy connections
                else if (UIEvent == EventType.MouseUp) // If the mouse button is released
                {
                    // If right button is released and selectionNode is not empty
                    if (currentEvent.button == 1 && selectedNode != null && selectedNode != targetTree.tree[nodeIdx])
                    {
                        // Remove any connection between the selectedNode and hovered node if exists
                        if (targetTree.tree[nodeIdx].requirements.Contains(selectedNode.tech))
                            targetTree.tree[nodeIdx].requirements.Remove(selectedNode.tech);
                        else if (selectedNode.requirements.Contains(targetTree.tree[nodeIdx].tech))
                            selectedNode.requirements.Remove(targetTree.tree[nodeIdx].tech);
                        // If doesn't exist and they are connectible then create a connection
                        else if (targetTree.IsConnectible(targetTree.tree.IndexOf(selectedNode), nodeIdx))
                        {
                            targetTree.tree[nodeIdx].requirements.Add(selectedNode.tech);

                            // Creating connection may annul other requirement connections, so check all connections.
                            for (int k = 0; k < targetTree.tree.Count; k++)
                                targetTree.CorrectRequirementCascades(k);
                        }                
                    }
                }
            }
        }

        // Scroll in the Techtree view
        if (currentEvent.button == 2) // If the middle mouse button is pressed, held, or released
        {
            if (currentEvent.type == EventType.MouseDown) // If the mouse button is down
                scrollStartPos = (currentEvent.mousePosition + scrollPosition); // Store the coordinate
            else if (currentEvent.type == EventType.MouseDrag) // If the mouse button is held
            {
                scrollPosition = -(currentEvent.mousePosition - scrollStartPos); // Recalculate scrollPosition. This moves everything
                Repaint(); // Repaint the GUI
            }
        }

        // Draw guiding connection when right mouse button is held
        if (selectedNode != null && currentEvent.button == 1) // If rightmouse button is used and selection is not empty.
        {
            // Draw connection guide between selected node and the mouse position.
            Handles.DrawBezier(currentEvent.mousePosition,
            selectedNode.UIposition - scrollPosition + incomingEdgeVec,
            currentEvent.mousePosition + Vector2.left * 100,
            selectedNode.UIposition - scrollPosition + incomingEdgeVec + Vector2.right * 100,
            Color.white, null, 1.5f);
            Repaint();            
        }

        // Move nodes with the left mouse button
        if (UIEvent == EventType.MouseUp) // If dropped
        {
            activeNode = null;
        }
        else if (UIEvent == EventType.MouseDrag) // While dragged
        {
            if (activeNode != null)
                activeNode.UIposition = currentEvent.mousePosition + mouseSelectionOffset;
        }

        // Import new Tech
        if (currentEvent.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
        else if (currentEvent.type == EventType.DragPerform)
        {
            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                if (DragAndDrop.objectReferences[i] is Tech)
                    targetTree.AddNode(DragAndDrop.objectReferences[i] as Tech, currentEvent.mousePosition + scrollPosition);
            }
        }
        EditorGUILayout.EndScrollView();

        scrollPosition.x = GUILayout.HorizontalScrollbar(scrollPosition.x, 20.0f, 0.0f, minTreeWidth);
        scrollPosition.y = GUI.VerticalScrollbar(new Rect(0, 0, 20, 720), scrollPosition.y, 20f, 0f, minTreeHeight);

        // Shows selecetd node tech and gives option to delete node
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
        EditorGUILayout.EndHorizontal();

        EditorUtility.SetDirty(targetTree); // Makes sure changes are persistent.
    }
}
