using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class IconGenerator : MonoBehaviour
{
    public List<GameObject> sceneObjects;
    public string pathFolder;
    public string suffix = "_icon";
    public int size = 256;

    private Camera theCamera = null;

    private void Awake()
    {
        theCamera = GetComponent<Camera>();
    }

    [ContextMenu("Screenshot")]
    private void ProcessScreenShots()
    {
        StartCoroutine(Screenshot());
    }

    private IEnumerator Screenshot()
    {
        for (int i = 0; i < sceneObjects.Count; i++)
        {
            GameObject obj = sceneObjects[i];

            obj.gameObject.SetActive(true);

            yield return null;

            TakeScreenshot($"{Application.dataPath}/{pathFolder}/{sceneObjects[i].name}{suffix}.png");

            yield return null;
            obj.gameObject.SetActive(false);

            // Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Application.dataPath}/{pathFolder}/{sceneObjects[i].name}_Icon.png");
            // if (s != null)
            // {
                
            // }

            yield return null;
        }
    }
    void TakeScreenshot(string fullPath)
    {
        if (theCamera == null)
        {
            theCamera = GetComponent<Camera>();
        }

        RenderTexture rt = new RenderTexture(size, size, 24);
        theCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(size, size, TextureFormat.RGBA32, false);
        theCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        theCamera.targetTexture = null;
        RenderTexture.active = null;

        if (Application.isEditor)
        {
            DestroyImmediate(rt);
        }
        else
        {
            Destroy(rt);
        }
        byte[] bytes = screenShot.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, bytes);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif

    }
}
