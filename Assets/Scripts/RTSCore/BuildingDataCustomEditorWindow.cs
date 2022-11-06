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
    GameObject constructionPrefabStage1;
    GameObject constructionPrefabStage2;
    GameObject constructionStage3;
    Material skinMaterial;
    Material fadedMaterial;
    SteamVR_Skeleton_Pose grabPose;
    SteamVR_Skeleton_Pose pinchPose;
    Vector2 boundingDimensions = Vector2.one;

    public void OnGUI()
    {
        bData = (BuildingData)EditorGUILayout.ObjectField("Building Data", bData, typeof(BuildingData), false); ;

        EditorGUILayout.TextField("Title", bData.title);
        EditorGUILayout.TextField("Description", bData.description);
        EditorGUILayout.FloatField("Queue Research Time", bData.queueResearchTime);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("World Visuals", EditorStyles.boldLabel);
        EditorGUILayout.ObjectField("World Prefab", bData.worldPrefab, typeof(GameObject), false);
        EditorGUILayout.ObjectField("World Button Material", bData.worldButtonMaterial, typeof(Material), false);

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
        EditorGUILayout.ObjectField("Construction", bData.constructablePrefab, typeof(GameObject), false);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Economic Costs", EditorStyles.boldLabel);
        EditorGUILayout.IntField("Population Cost", bData.populationCost);
        EditorGUILayout.IntField("Gold Cost", bData.goldCost);
        EditorGUILayout.IntField("Stone Cost", bData.stoneCost);
        EditorGUILayout.IntField("Food Cost", bData.foodCost);
        EditorGUILayout.IntField("Wood Cost", bData.woodCost);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Building Settings", EditorStyles.boldLabel);
        EditorGUILayout.EnumPopup("Building Type",bData.buildingType);
        EditorGUILayout.EnumFlagsField("Dropoff Types", bData.dropoffTypes);
        EditorGUILayout.Vector2Field("Bounding Dimensions", new Vector2(bData.boundingDimensionX, bData.boundingDimensionY));

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        EditorGUILayout.IntField("Population Supported", bData.populationSupported);
        EditorGUILayout.IntField("Max Unit Queue Size", bData.maxUnitQueueSize);
        EditorGUILayout.IntField("Maximum Hit Points", bData.maximumHitPoints);
        EditorGUILayout.IntField("Armor", bData.armor);

        EditorGUILayout.Space();

        //EditorGUILayout.LabelField("Wall Prefabs", EditorStyles.boldLabel);

        EditorGUILayout.Space();


        EditorGUILayout.LabelField("GENERATOR", EditorStyles.boldLabel);

        model = (GameObject)EditorGUILayout.ObjectField("Model", model, typeof(GameObject), true);
        skinMaterial = (Material)EditorGUILayout.ObjectField("Skin Material", skinMaterial, typeof(Material), true);
        fadedMaterial = (Material)EditorGUILayout.ObjectField("Faded Material", fadedMaterial, typeof(Material), true);

        constructionPrefabStage1 = (GameObject)EditorGUILayout.ObjectField("Construction Stage 1", constructionPrefabStage1, typeof(GameObject), true);
        constructionPrefabStage2 = (GameObject)EditorGUILayout.ObjectField("Construction Stage 2", constructionPrefabStage2, typeof(GameObject), true);

        grabPose = (SteamVR_Skeleton_Pose)EditorGUILayout.ObjectField("Grab Pose", grabPose, typeof(SteamVR_Skeleton_Pose), true);
        pinchPose = (SteamVR_Skeleton_Pose)EditorGUILayout.ObjectField("Pinch Pose", pinchPose, typeof(SteamVR_Skeleton_Pose), true);

        if (GUILayout.Button("Generate Visual Prefabs"))
        {
            string editedName = bData.title.Replace(' ', '_').ToLower();

            // Menu preview
            GameObject menuPreview = GenerateObject(editedName + "_menu_preview", model, skinMaterial);
            menuPreview.transform.localScale = new Vector3(0.125033662f, 0.125033662f, 0.125033662f);
            bData.menuPreviewPrefab = SavePrefabObject(menuPreview);

            // Faded preview
            GameObject fadedPreview = GenerateObject(editedName + "_faded_preview", model, fadedMaterial);
            bData.fadedPreviewPrefab = SavePrefabObject(fadedPreview);

            // World preview
            GameObject worldPreview = GenerateObject(editedName + "_world_preview", model, skinMaterial);
            bData.worldPreviewPrefab = SavePrefabObject(worldPreview);

            // Throwable
            GameObject throwable = GenerateThrowable(bData, model, editedName);

            // Construction
            GameObject construction = new GameObject(editedName + "_construction");
            construction.AddComponent<AudioSource>();
            construction.AddComponent<PointerInteractable>().highlightOnHover = true;
            construction.AddComponent<Interactable>();
            construction.AddComponent<SphereCollider>().radius = 0.3f;

            Constructible constructible = construction.AddComponent<Constructible>();
            constructible.BoundingDimensions = boundingDimensions;
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

            bData.constructablePrefab = SavePrefabObject(construction);

            // Cleanup
            DestroyImmediate(menuPreview);
            DestroyImmediate(fadedPreview);
            DestroyImmediate(worldPreview);
            DestroyImmediate(throwable);
            DestroyImmediate(construction);
        }

        EditorGUILayout.Space();
        //bData.worldPrefab = (GameObject)EditorGUILayout.ObjectField("Blah", bData.worldPrefab, typeof(GameObject), false);
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
        string path = "Assets/Prefabs/";

        if (!Directory.Exists(path)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        path += "Buildings/";

        if (!Directory.Exists(path)) AssetDatabase.CreateFolder("Prefabs", "Buildings");

        // Menu preview           
        string localPath = path + gameObject.name + ".prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        bool prefabSuccess = false;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, localPath, out prefabSuccess);

        if (prefabSuccess == true)
            Debug.Log("Prefab was saved successfully");
        else
            Debug.Log("Prefab failed to save" + prefabSuccess);

        return prefab;
    }
}
