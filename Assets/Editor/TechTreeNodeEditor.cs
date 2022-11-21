using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TechTreeNodeEditor : EditorWindow
{
    private TechTreeEditor techTreeEditor;

    [MenuItem("VRTS/Tech Tree Node Editor")]
    private static void OpenWindow()
    {
        TechTreeNodeEditor window = GetWindow<TechTreeNodeEditor>();
        window.titleContent = new GUIContent("Tech Tree Node Editor");
    }

    private void OnEnable()
    {
        techTreeEditor = EditorWindow.GetWindow<TechTreeEditor>();
    }

    TechNode currentNode;

    void OnInspectorUpdate() => Repaint();

    Vector2 scrollPosition = Vector2.zero;

    public void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Shows selected node tech and gives option to delete node
        EditorGUILayout.BeginVertical();

        if (techTreeEditor.selectedNode != null && techTreeEditor.selectedNode.tech != null)
                currentNode = techTreeEditor.selectedNode;               

        if (currentNode != null)
        {
            EditorGUILayout.LabelField("Selected: " + currentNode.tech.name);

            DrawTechBase(currentNode);

            if (currentNode is BuildingNode)
                DrawBuildingNodeEditor((BuildingNode)currentNode);
            else if (currentNode is EpochNode)
                DrawEpochNodeEditor((EpochNode)currentNode);
            else if (currentNode is UnitNode)
                DrawUnitNodeEditor((UnitNode)currentNode);
            else if (currentNode is ResearchNode)
                DrawResearchNodeEditor((ResearchNode)currentNode);
            else if (currentNode is UpgradeNode)
                DrawUpgradeNodeEditor((UpgradeNode)currentNode);

        }
        else
            EditorGUILayout.LabelField("Selected: none");

        ProcessEvents(Event.current);

        if (GUI.changed)
            Repaint();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private void ProcessEvents(Event e)
    {
        switch (e.type)
        {
            case EventType.DragUpdated: // Import new button
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                break;

            case EventType.DragPerform:
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    if (DragAndDrop.objectReferences[i] is TechBase)
                    {
                        AddDraggedInButton(DragAndDrop.objectReferences[i] as TechBase);
                    }
                }
                break;
        }
    }

    private void AddDraggedInButton(TechBase techBase)
    {
        if (currentNode != null)
        {
            if (currentNode is BuildingNode)
            {
                ((BuildingData)currentNode.tech).techQueueButtons.Add(techBase);
                GUI.changed = true;
            }
        }
    }

    private void DrawEpochNodeEditor(EpochNode node)
    {
        node.researched = EditorGUILayout.Toggle("Researched", node.researched);
        node.requiredBuildingCount = EditorGUILayout.IntField("Required Building Count", node.requiredBuildingCount);
    }

    private void DrawUnitNodeEditor(UnitNode node)
    {
    
    }

    private void DrawResearchNodeEditor(ResearchNode node)
    {
        node.researched = EditorGUILayout.Toggle("Researched", node.researched);
    }

    private void DrawUpgradeNodeEditor(UpgradeNode node)
    {
        node.researched = EditorGUILayout.Toggle("Researched", node.researched);
    }

    private void DrawTechBase(TechNode node)
    {
        TechBase techBase = node.tech;

        techBase.title = EditorGUILayout.TextField("Title", techBase.title);
        techBase.description = EditorGUILayout.TextField("Description", techBase.description);
        techBase.queueResearchTime = EditorGUILayout.FloatField("Queue Research Time", techBase.queueResearchTime);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("World Visuals", EditorStyles.boldLabel);
        techBase.worldPrefab = (GameObject)EditorGUILayout.ObjectField("World Prefab", techBase.worldPrefab, typeof(GameObject), false);
        techBase.worldButtonMaterial = (Material)EditorGUILayout.ObjectField("World Button Material", techBase.worldButtonMaterial, typeof(Material), false);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("World Queue Image");
        techBase.worldQueueImage = (Sprite)EditorGUILayout.ObjectField(techBase.worldQueueImage, typeof(Sprite), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Economic Costs", EditorStyles.boldLabel);
        techBase.populationCost = EditorGUILayout.IntField("Population Cost", techBase.populationCost);
        techBase.goldCost = EditorGUILayout.IntField("Gold Cost", techBase.goldCost);
        techBase.stoneCost = EditorGUILayout.IntField("Stone Cost", techBase.stoneCost);
        techBase.foodCost = EditorGUILayout.IntField("Food Cost", techBase.foodCost);
        techBase.woodCost = EditorGUILayout.IntField("Wood Cost", techBase.woodCost);

        EditorGUILayout.Space();
    }

    private void DrawBuildingNodeEditor(BuildingNode node)
    {
        BuildingData buildingData = (BuildingData)node.tech;

        EditorGUILayout.LabelField("Additional Visual Prefabs", EditorStyles.boldLabel);
        buildingData.menuPreviewPrefab = EditorGUILayout.ObjectField("Menu Preview", buildingData.menuPreviewPrefab, typeof(GameObject), false) as GameObject;
        buildingData.fadedPreviewPrefab = EditorGUILayout.ObjectField("Faded Preview", buildingData.fadedPreviewPrefab, typeof(GameObject), false) as GameObject;
        buildingData.worldPreviewPrefab = EditorGUILayout.ObjectField("World Preview", buildingData.worldPreviewPrefab, typeof(GameObject), false) as GameObject;
        buildingData.throwablePrefab = EditorGUILayout.ObjectField("Throwable", buildingData.throwablePrefab, typeof(GameObject), false) as GameObject;
        buildingData.constructionPrefab = EditorGUILayout.ObjectField("Construction", buildingData.constructionPrefab, typeof(GameObject), false) as GameObject;

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Economic Costs", EditorStyles.boldLabel);
        buildingData.populationCost = EditorGUILayout.IntField("Population Cost", buildingData.populationCost);
        buildingData.goldCost = EditorGUILayout.IntField("Gold Cost", buildingData.goldCost);
        buildingData.stoneCost = EditorGUILayout.IntField("Stone Cost", buildingData.stoneCost);
        buildingData.foodCost = EditorGUILayout.IntField("Food Cost", buildingData.foodCost);
        buildingData.woodCost = EditorGUILayout.IntField("Wood Cost", buildingData.woodCost);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Building Settings", EditorStyles.boldLabel);
        buildingData.buildingType = (BuildingType)EditorGUILayout.EnumPopup("Building Type", buildingData.buildingType);
        buildingData.dropoffTypes = (ResourceGatheringType)EditorGUILayout.EnumFlagsField("Dropoff Types", buildingData.dropoffTypes);
        buildingData.boundingDimensionX = EditorGUILayout.IntField("Bounding Dimensions X", buildingData.boundingDimensionX);
        buildingData.boundingDimensionY = EditorGUILayout.IntField("Bounding Dimensions Y", buildingData.boundingDimensionY);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        buildingData.populationSupported = EditorGUILayout.IntField("Population Supported", buildingData.populationSupported);
        buildingData.maxUnitQueueSize = EditorGUILayout.IntField("Max Unit Queue Size", buildingData.maxUnitQueueSize);
        buildingData.maximumHitPoints = EditorGUILayout.IntField("Maximum Hit Points", buildingData.maximumHitPoints);
        buildingData.armor = EditorGUILayout.IntField("Armor", buildingData.armor);

        EditorGUILayout.Space();

        buildingData.constructionCompletedAudio = (Swordfish.Audio.SoundElement)EditorGUILayout.ObjectField("Construction Completed Audio", buildingData.constructionCompletedAudio, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        
        node.isBuilt = EditorGUILayout.Toggle("IsBuilt", node.isBuilt);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Buttons", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        
        foreach(TechBase tech in buildingData.techQueueButtons)
        {
            if (tech.worldQueueImage != null)
            {
                if (GUILayout.Button(tech.worldQueueImage.texture, GUILayout.Width(64.0f), GUILayout.Height(64.0f)))
                {
                    if (Event.current.button == 0)
                    {
                        Selection.activeObject = tech;
                    }
                    if (Event.current.button == 1)
                        ProcessContextMenu(tech);                        
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void OnClickRemoveButton(object techBase)
    {
        ((BuildingData)currentNode.tech).techQueueButtons.Remove((TechBase)techBase);
    }

    private void ProcessContextMenu(TechBase techBase)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Remove Button"), false, OnClickRemoveButton, techBase);
        genericMenu.ShowAsContext();
    }
}