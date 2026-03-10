using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using System.IO;
using UnityEditor;
using System.Text.RegularExpressions; // Required for AssetDatabase and PrefabUtility


public class AutoSpriteBaker : MonoBehaviour
{
    [Header("Studio Setup")]
    public Camera captureCamera;
    public Transform spawnPoint;
    public Vector3 rotationOffset = new Vector3(0, 45, 0);
    public int resolution = 1024;
    public string saveFolder = "BakedSprites";

    [Header("Manual Trimming (%)")]
    [Range(0, 45)] public float trimLeft = 10f;
    [Range(0, 45)] public float trimRight = 10f;
    [Range(0, 45)] public float trimTop = 10f;
    [Range(0, 45)] public float trimBottom = 10f;
    public GameObject CullObject;

    [ContextMenu("Run Full Batch Capture")]
    public void StartBatch()
    {
        StartCoroutine(BatchRoutine());
    }

    private IEnumerator BatchRoutine()
    {
        if (DataManager.Instance == null) { Debug.LogError("DataManager missing!"); yield break; }

        DataManager.Instance.LoadMetadata();
        yield return new WaitForEndOfFrame();

        var items = DataManager.Instance.Metadata.Items;
        string folderPath = Path.Combine(Application.dataPath, saveFolder);

        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        foreach (var item in items)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(item.PrefabName);
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject instance = Instantiate(handle.Result);
                instance.transform.localScale = Vector3.one;
                instance.transform.rotation = Quaternion.Euler(rotationOffset);

                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                yield return new WaitForSeconds(0.1f);

                // Pass just the ID; the prefix is handled inside CapturePNG
                CapturePNG(item.Id.ToString());

                DestroyImmediate(instance);
                Addressables.Release(handle);
            }
        }

        Debug.Log("--- BATCH CAPTURE COMPLETE ---");
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    private void CapturePNG(string id)
    {
        CullObject.SetActive(false);
        RenderTexture rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        captureCamera.targetTexture = rt;
        captureCamera.backgroundColor = new Color(0, 0, 0, 0);
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.Render();

        RenderTexture.active = rt;
        Texture2D fullShot = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        fullShot.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        fullShot.Apply();

        // 1. DETACH CAMERA FIRST (Fixes the warning)
        captureCamera.targetTexture = null;
        RenderTexture.active = null;

        // --- MANUAL SLIDER CROPPING ---
        int leftPx = Mathf.FloorToInt(resolution * (trimLeft / 100f));
        int rightPx = Mathf.FloorToInt(resolution * (trimRight / 100f));
        int topPx = Mathf.FloorToInt(resolution * (trimTop / 100f));
        int bottomPx = Mathf.FloorToInt(resolution * (trimBottom / 100f));

        int newWidth = resolution - leftPx - rightPx;
        int newHeight = resolution - bottomPx - topPx;

        Texture2D cropped = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        Color[] pixels = fullShot.GetPixels(leftPx, bottomPx, newWidth, newHeight);
        cropped.SetPixels(pixels);
        cropped.Apply();

        // 2. SAVE TO CORRECT FOLDER
        byte[] bytes = cropped.EncodeToPNG();
        string fileName = "icon_" + id + ".png";
        string path = Path.Combine(Application.dataPath, saveFolder, fileName);
        File.WriteAllBytes(path, bytes);

        // 3. CLEANUP
        DestroyImmediate(rt);
        DestroyImmediate(fullShot);
        DestroyImmediate(cropped);
        CullObject.SetActive(true);
    }

    [ContextMenu("Capture Current View")]
    public void CaptureCurrentView()
    {
        // Ensure the folder exists before capturing
        string folderPath = Path.Combine(Application.dataPath, saveFolder);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // Generate a unique timestamped ID so you don't overwrite previous captures
        string timestampId = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // Use your existing logic to render and save
        CapturePNG("manual_");

        Debug.Log($"<color=green>Manual Capture Saved:</color> manual_{timestampId}.png");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }


    // ... inside your class ...
    [ContextMenu("Process Prefabs & Format Names")]
    public void ProcessPrefabsAndFormatNames()
    {
        string folderPath = "Assets/Prefabs/Items"; // Path to your prefabs
        string txtPath = Path.Combine(Application.dataPath, "PrefabNames.txt");

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        StringBuilder formattedNamesSummary = new StringBuilder();
        formattedNamesSummary.AppendLine("\n--- Formatted Names List ---");

        using (StreamWriter writer = new StreamWriter(txtPath))
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    // 1. Write original name to the file
                    writer.WriteLine(prefab.name);

                    // 2. Generate the formatted name (e.g., Pot01 -> pot_01)
                    string formattedName = FormatToToonStyle(prefab.name);
                    formattedNamesSummary.AppendLine(formattedName);

                    // 3. Update the Prefab's ClickableItem rotation
                    GameObject contents = PrefabUtility.LoadPrefabContents(path);
                    if (contents.TryGetComponent<ClickableItem>(out var clickable))
                    {
                        clickable.Rotation = rotationOffset;
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                    }
                    PrefabUtility.UnloadPrefabContents(contents);
                }

                // break; // Uncomment this to test with just one item
            }

            // 4. Append the lowercase formatted list at the end of the file
            writer.Write(formattedNamesSummary.ToString());
        }

        AssetDatabase.Refresh();
        Debug.Log($"Processed {guids.Length} items. Formatting complete in PrefabNames.txt");
    }

    private string FormatToToonStyle(string original)
    {
        // 1. Insert underscore between lowercase and uppercase (CamelCase): GoldenKey -> Golden_Key
        string camelCaseSeparated = Regex.Replace(original, @"([a-z])([A-Z])", "$1_$2");

        // 2. Insert underscore between letters and numbers: Key05 -> Key_05
        string digitSeparated = Regex.Replace(camelCaseSeparated, @"([a-zA-Z])(\d)", "$1_$2");

        // 3. Convert to lowercase: Golden_Key_05 -> golden_key_05
        return digitSeparated.ToLower();
    }
}