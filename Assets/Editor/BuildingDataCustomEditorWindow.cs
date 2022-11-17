using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using Valve.VR.InteractionSystem;
using Valve.VR;

public class BuildingDataCustomEditorWindow : EditorWindow
{
    [MenuItem("Window/Building Data Editor")]
    private static void OpenWindow()
    {
        BuildingDataCustomEditorWindow window = GetWindow<BuildingDataCustomEditorWindow>();
        window.titleContent = new GUIContent("Building Data Editor");
    }

    BuildingData bData;
    GameObject model;
    GameObject rallyPointPrefab;
    GameObject constructionPrefabStage1;
    GameObject constructionPrefabStage2;
    GameObject constructionStage3;
    Material skinMaterial;
    Material fadedMaterial;
    SteamVR_Skeleton_Pose grabPose;
    SteamVR_Skeleton_Pose pinchPose;
    int boundingDimensionX;
    int boundingDimensionY;
    Vector2 scrollPosition = Vector2.zero;

    public void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        bData = (BuildingData)EditorGUILayout.ObjectField("Building Data", bData, typeof(BuildingData), false);
        
        if (bData)
        {
            DrawEditor();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEditor()
    {
        bData.title = EditorGUILayout.TextField("Title", bData.title);
        bData.description = EditorGUILayout.TextField("Description", bData.description);
        bData.queueResearchTime = EditorGUILayout.FloatField("Queue Research Time", bData.queueResearchTime);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("World Visuals", EditorStyles.boldLabel);
        bData.worldPrefab = (GameObject)EditorGUILayout.ObjectField("World Prefab", bData.worldPrefab, typeof(GameObject), false);

        bData.worldButtonMaterial = (Material)EditorGUILayout.ObjectField("World Button Material", bData.worldButtonMaterial, typeof(Material), false);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("World Queue Image");
        bData.worldQueueImage = (Sprite)EditorGUILayout.ObjectField(bData.worldQueueImage, typeof(Sprite), false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Additional Visual Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.ObjectField("Menu Preview", bData.menuPreviewPrefab, typeof(GameObject), false);
        EditorGUILayout.ObjectField("Faded Preview", bData.fadedPreviewPrefab, typeof(GameObject), false);
        EditorGUILayout.ObjectField("World Preview", bData.worldPreviewPrefab, typeof(GameObject), false);
        EditorGUILayout.ObjectField("Throwable", bData.throwablePrefab, typeof(GameObject), false);
        EditorGUILayout.ObjectField("Construction", bData.constructionPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Economic Costs", EditorStyles.boldLabel);
        bData.populationCost = EditorGUILayout.IntField("Population Cost", bData.populationCost);
        bData.goldCost = EditorGUILayout.IntField("Gold Cost", bData.goldCost);
        bData.stoneCost = EditorGUILayout.IntField("Stone Cost", bData.stoneCost);
        bData.foodCost = EditorGUILayout.IntField("Food Cost", bData.foodCost);
        bData.woodCost = EditorGUILayout.IntField("Wood Cost", bData.woodCost);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Building Settings", EditorStyles.boldLabel);
        bData.buildingType = (RTSBuildingType)EditorGUILayout.EnumPopup("Building Type", bData.buildingType);
        bData.dropoffTypes = (ResourceGatheringType)EditorGUILayout.EnumFlagsField("Dropoff Types", bData.dropoffTypes);
        bData.boundingDimensionX = EditorGUILayout.IntField("Bounding Dimensions X", bData.boundingDimensionX);
        bData.boundingDimensionY = EditorGUILayout.IntField("Bounding Dimensions Y", bData.boundingDimensionY);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        bData.populationSupported = EditorGUILayout.IntField("Population Supported", bData.populationSupported);
        bData.maxUnitQueueSize = EditorGUILayout.IntField("Max Unit Queue Size", bData.maxUnitQueueSize);
        bData.maximumHitPoints = EditorGUILayout.IntField("Maximum Hit Points", bData.maximumHitPoints);
        bData.armor = EditorGUILayout.IntField("Armor", bData.armor);

        EditorGUILayout.Space();

        //EditorGUILayout.LabelField("Wall Prefabs", EditorStyles.boldLabel);

        EditorGUILayout.Space();


        EditorGUILayout.LabelField("GENERATOR", EditorStyles.boldLabel);

        model = (GameObject)EditorGUILayout.ObjectField("Model", model, typeof(GameObject), true);
        skinMaterial = (Material)EditorGUILayout.ObjectField("Skin Material", skinMaterial, typeof(Material), true);
        fadedMaterial = (Material)EditorGUILayout.ObjectField("Faded Material", fadedMaterial, typeof(Material), true);

        rallyPointPrefab = (GameObject)EditorGUILayout.ObjectField("Rally Point Prefab", rallyPointPrefab, typeof(GameObject), true);

        constructionPrefabStage1 = (GameObject)EditorGUILayout.ObjectField("Construction Stage 1", constructionPrefabStage1, typeof(GameObject), true);
        constructionPrefabStage2 = (GameObject)EditorGUILayout.ObjectField("Construction Stage 2", constructionPrefabStage2, typeof(GameObject), true);

        grabPose = (SteamVR_Skeleton_Pose)EditorGUILayout.ObjectField("Grab Pose", grabPose, typeof(SteamVR_Skeleton_Pose), true);
        pinchPose = (SteamVR_Skeleton_Pose)EditorGUILayout.ObjectField("Pinch Pose", pinchPose, typeof(SteamVR_Skeleton_Pose), true);

        int worldLayer = LayerMask.NameToLayer("Building");
        worldLayer = EditorGUILayout.LayerField("World Layer", worldLayer);

        int menuPreviewLayer = LayerMask.NameToLayer("UI");
        menuPreviewLayer = EditorGUILayout.LayerField("Menu Preview Layer", menuPreviewLayer);

        int fadedPreviewLayer = LayerMask.NameToLayer("UI");
        fadedPreviewLayer = EditorGUILayout.LayerField("Faded Preview Layer", fadedPreviewLayer);

        int worldPreviewLayer = LayerMask.NameToLayer("UI");
        worldPreviewLayer = EditorGUILayout.LayerField("World Preview Layer", worldPreviewLayer);

        int throwableLayer = LayerMask.NameToLayer("ThrowableBuilding");
        throwableLayer = EditorGUILayout.LayerField("Throwable Layer", throwableLayer);

        int constructionLayer = LayerMask.NameToLayer("Building");
        constructionLayer = EditorGUILayout.LayerField("Construction Layer", constructionLayer);

        if (GUILayout.Button("Generate Visual Prefabs"))
        {
            string editedName = bData.name.Replace(' ', '_').ToLower();

            // World prefab exists
            if (bData.worldPrefab != null)
                bData.worldPrefab.layer = worldLayer;
            // Generate world prefab
            else
            {
                GameObject worldObject = new GameObject(editedName + "_world");
                worldObject.layer = worldLayer;
                worldObject.AddComponent<AudioSource>();
                worldObject.AddComponent<PointerInteractable>();

                GameObject displayModel = Instantiate(model, Vector3.zero, Quaternion.identity, worldObject.transform);
                GameObject rallyPointModel = Instantiate(rallyPointPrefab, Vector3.zero, Quaternion.identity, worldObject.transform);
                
                GameObject spawnPoint = new GameObject("spawnPoint");
                spawnPoint.transform.SetParent(worldObject.transform);

                Structure structure = worldObject.AddComponent<Structure>();
                structure.buildingData = bData;
                structure.BoundingDimensions.x = boundingDimensionX;
                structure.BoundingDimensions.y = boundingDimensionY;
                structure.SkinRendererTargets = new Renderer[1];
                structure.SkinRendererTargets[0] = displayModel.GetComponent<MeshRenderer>();

                worldObject.AddComponent<Swordfish.Damageable>();
                worldObject.AddComponent<Interactable>().highlightOnHover = false;
                
                SphereCollider collider = worldObject.AddComponent<SphereCollider>();
                collider.radius = 0.21f;
                collider.center = new Vector3(0.0f, 0.06f, 0.0f);

                BuildingInteractionPanel interactionPanel = worldObject.AddComponent<BuildingInteractionPanel>();
                // interactionPanel.healthBarBackground = null;
                // interactionPanel.healthBarForeground = null;

                // interactionPanel.progressImage = null;
                // interactionPanel.progressFont = null;

                // interactionPanel.buttonBaseMaterial = null;
                // interactionPanel.cancelButtonMaterial = null;
                // interactionPanel.onButtonDownAudio = null;
                // interactionPanel.onButtonUpAudio = null;
                // interactionPanel.emptyQueueSlotSprite = null;
                // interactionPanel.buttonLockPrefab = null;

                interactionPanel.unitRallyWaypoint = rallyPointModel.transform;
                interactionPanel.unitSpawnPoint = spawnPoint.transform;

                bData.worldPrefab = SavePrefabObject(worldObject);
            }

            // Menu preview
            GameObject menuPreview = GenerateObject(editedName + "_menu_preview", model, skinMaterial);
            menuPreview.transform.localScale = new Vector3(0.125033662f, 0.125033662f, 0.125033662f);
            menuPreview.layer = menuPreviewLayer;
            bData.menuPreviewPrefab = SavePrefabObject(menuPreview);

            // Faded preview
            GameObject fadedPreview = GenerateObject(editedName + "_faded_preview", model, fadedMaterial);
            fadedPreview.layer = LayerMask.NameToLayer("UI");
            bData.fadedPreviewPrefab = SavePrefabObject(fadedPreview);

            // World preview
            GameObject worldPreview = GenerateObject(editedName + "_world_preview", model, skinMaterial);
            worldPreview.layer = worldPreviewLayer;
            bData.worldPreviewPrefab = SavePrefabObject(worldPreview);

            // Throwable
            GameObject throwable = GenerateThrowable(bData, model, editedName);
            throwable.layer = throwableLayer;

            // Construction
            GameObject construction = new GameObject(editedName + "_construction");
            construction.layer = constructionLayer;
            construction.AddComponent<AudioSource>();
            construction.AddComponent<PointerInteractable>().highlightOnHover = true;
            construction.AddComponent<Interactable>();
            construction.AddComponent<SphereCollider>().radius = 0.3f;

            Constructible constructible = construction.AddComponent<Constructible>();
            constructible.BoundingDimensions.x = boundingDimensionX;
            constructible.BoundingDimensions.y = boundingDimensionY;
            constructible.buildingData = bData;
            constructible.OnBuiltPrefab = bData.worldPrefab;

            GameObject stage1 = Instantiate(constructionPrefabStage1);
            stage1.GetComponent<MeshRenderer>().sharedMaterial = skinMaterial;
            stage1.transform.SetParent(construction.transform);

            GameObject stage2 = Instantiate(constructionPrefabStage2);
            stage2.GetComponent<MeshRenderer>().sharedMaterial = skinMaterial;
            stage2.transform.SetParent(construction.transform);

            GameObject stage3 = GenerateObject(editedName + "_2", model, skinMaterial);
            stage3.GetComponent<MeshRenderer>().sharedMaterial = skinMaterial;
            stage3.transform.SetParent(construction.transform);

            constructible.ConstructionStages = new GameObject[3];
            constructible.ConstructionStages[0] = stage1;
            constructible.ConstructionStages[1] = stage2;
            constructible.ConstructionStages[2] = stage3;

            bData.constructionPrefab = SavePrefabObject(construction);

            EditorUtility.SetDirty(bData);

            // Cleanup
            DestroyImmediate(menuPreview);
            DestroyImmediate(fadedPreview);
            DestroyImmediate(worldPreview);
            DestroyImmediate(throwable);
            DestroyImmediate(construction);
        }
    }

    private GameObject GenerateThrowable(BuildingData bData, GameObject model, string name)
    {
        GameObject throwable = GenerateObject(name + "_throwable", model, skinMaterial);
        throwable.transform.localScale = new Vector3(0.125033662f, 0.125033662f, 0.125033662f);
        Interactable interactable = throwable.AddComponent<Interactable>();
        interactable.hideHandOnAttach = false;
        interactable.handFollowTransform = false;
        interactable.highlightOnHover = false;
        Rigidbody rigidBody = throwable.AddComponent<Rigidbody>();
        throwable.AddComponent<SphereCollider>();

        ThrowableBuilding throwBuilding = throwable.AddComponent<ThrowableBuilding>();
        throwBuilding.attachmentFlags = Hand.AttachmentFlags.DetachFromOtherHand | Hand.AttachmentFlags.ParentToHand | Hand.AttachmentFlags.TurnOnKinematic;

        SteamVR_Skeleton_Poser poser = throwable.AddComponent<SteamVR_Skeleton_Poser>();
        poser.skeletonMainPose = grabPose;
        poser.skeletonAdditionalPoses.Add(pinchPose);
        SteamVR_Skeleton_Poser.PoseBlendingBehaviour blend = new SteamVR_Skeleton_Poser.PoseBlendingBehaviour();
        poser.blendingBehaviours.Add(blend);
        blend.name = "PinchPose";
        blend.enabled = false;
        blend.influence = 1.0f;
        blend.pose = 1;
        blend.value = 1.0f;
        blend.type = SteamVR_Skeleton_Poser.PoseBlendingBehaviour.BlenderTypes.Manual;

        bData.throwablePrefab = SavePrefabObject(throwable);

        return throwable;
    }

    private GameObject GenerateObject(string name, GameObject model, Material material)
    {
        GameObject gameObject = Instantiate<GameObject>(model);
        gameObject.name = name;
        gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;

        return gameObject;
    }

    private GameObject SavePrefabObject(GameObject gameObject)
    {
        string path = "Assets/Prefabs/Buildings";

        if (!Directory.Exists(path)) AssetDatabase.CreateFolder("Assets/Prefabs", "Buildings");
        path += "/Generated";
        if (!Directory.Exists(path)) AssetDatabase.CreateFolder("Assets/Prefabs/Buildings", "Generated");
                
        if (!Directory.Exists(path))
        {
            Debug.LogError("Path failure");
            return null;
        }

        string localPath = path + "/" + gameObject.name + ".prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        bool prefabSuccess = false;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, localPath, out prefabSuccess);

        if (prefabSuccess == true)
            Debug.LogFormat("Prefab was saved successfully: {0}", localPath);
        else
            Debug.Log("Prefab failed to save" + prefabSuccess);

        return prefab;
    }
}
