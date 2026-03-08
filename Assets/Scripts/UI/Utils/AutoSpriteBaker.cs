using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using System.IO;

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
    }
}