using UnityEngine;
using UnityEditor;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class AutoSpriteBakerEditor : EditorWindow
{
    [Header("Studio Setup")]
    public Camera captureCamera;
    public Transform spawnParent; // The parent object in your scene
    public List<GameObject> studioStageObjects = new List<GameObject>();
    public int resolution = 1024;
    public string saveFolder = "BakedSpritesNewer";

    [Header("Manual Trimming (%)")]
    [Range(0, 45)] public float trimLeft = 10f;
    [Range(0, 45)] public float trimRight = 10f;
    [Range(0, 45)] public float trimTop = 10f;
    [Range(0, 45)] public float trimBottom = 10f;

    private Metadata metadata;
    private SerializedObject so;

    [MenuItem("Tools/Sprite Baker Studio")]
    public static void ShowWindow()
    {
        GetWindow<AutoSpriteBakerEditor>("Sprite Baker Studio");
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);
    }

    private void OnGUI()
    {
        so.Update();

        GUILayout.Label("Hardware & Stage", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("captureCamera"));
        EditorGUILayout.PropertyField(so.FindProperty("spawnParent"), new GUIContent("Spawn Parent (Container)"));
        EditorGUILayout.PropertyField(so.FindProperty("studioStageObjects"), true);

        GUILayout.Space(5);
        GUILayout.Label("File Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("resolution"));
        EditorGUILayout.PropertyField(so.FindProperty("saveFolder"));

        GUILayout.Space(10);
        GUILayout.Label("Cropping Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("trimLeft"));
        EditorGUILayout.PropertyField(so.FindProperty("trimRight"));
        EditorGUILayout.PropertyField(so.FindProperty("trimTop"));
        EditorGUILayout.PropertyField(so.FindProperty("trimBottom"));

        GUILayout.Space(20);

        if (GUILayout.Button("Run Full Addressable Batch", GUILayout.Height(40)))
        {
            RunAddressableBatch();
        }

        so.ApplyModifiedProperties();
    }

    private void RunAddressableBatch()
    {
        if (captureCamera == null) { Debug.LogError("Capture Camera is missing!"); return; }

        LoadMetadataManually("Metadata");

        if (metadata == null || metadata.Items == null) return;

        string folderPath = Path.Combine(Application.dataPath, saveFolder);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        int total = metadata.Items.Count;

        try
        {
            for (int i = 0; i < total; i++)
            {
                var item = metadata.Items[i];
                EditorUtility.DisplayProgressBar("Baking Addressables",
                    $"Instantiating: {item.PrefabName} ({i + 1}/{total})",
                    (float)i / total);

                // Load Asset
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(item.PrefabName);
                GameObject prefab = handle.WaitForCompletion();

                if (prefab != null)
                {
                    // Instantiate inside the Parent
                    GameObject instance = Instantiate(prefab);

                    // If no parent assigned, it defaults to world origin, 
                    // otherwise it uses the parent's local zero.
                    // instance.transform.localPosition = Vector3.zero;
                    // instance.transform.localScale = Vector3.one;

                    // Apply ClickableItem Data
                    if (instance.TryGetComponent<ClickableItem>(out var clickableItem))
                    {
                        instance.transform.rotation = Quaternion.Euler(clickableItem.Rotation);
                        instance.transform.position = clickableItem.Position;
                    }
                    // Physics safety
                    Rigidbody rb = instance.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                    }

                    // Render & Save
                    CapturePNG(item.Id.ToString());

                    // Cleanup
                    DestroyImmediate(instance);
                    Addressables.Release(handle);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log("--- BATCH COMPLETE ---");
        }
    }

    private void LoadMetadataManually(string fileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);
        if (jsonFile == null) return;
        metadata = JsonConvert.DeserializeObject<Metadata>(jsonFile.text);
    }

    private void CapturePNG(string id)
    {
        RenderTexture rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        captureCamera.targetTexture = rt;
        captureCamera.backgroundColor = new Color(0, 0, 0, 0);
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.Render();

        RenderTexture.active = rt;
        Texture2D fullShot = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        fullShot.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        fullShot.Apply();

        captureCamera.targetTexture = null;
        RenderTexture.active = null;

        // Crop Math
        int leftPx = Mathf.FloorToInt(resolution * (trimLeft / 100f));
        int rightPx = Mathf.FloorToInt(resolution * (trimRight / 100f));
        int topPx = Mathf.FloorToInt(resolution * (trimTop / 100f));
        int bottomPx = Mathf.FloorToInt(resolution * (trimBottom / 100f));
        int newWidth = Mathf.Max(1, resolution - leftPx - rightPx);
        int newHeight = Mathf.Max(1, resolution - bottomPx - topPx);

        Texture2D cropped = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        cropped.SetPixels(fullShot.GetPixels(leftPx, bottomPx, newWidth, newHeight));
        cropped.Apply();

        byte[] bytes = cropped.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, saveFolder, $"icon_{id}.png");
        File.WriteAllBytes(path, bytes);

        DestroyImmediate(rt);
        DestroyImmediate(fullShot);
        DestroyImmediate(cropped);
    }
}