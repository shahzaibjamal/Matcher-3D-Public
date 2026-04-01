using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Newtonsoft.Json;

public class PreBuildScaleProcessor
{
    // Path to the JSON that stores "Ground Truth" scales
    private static string ScaleDbPath => Path.Combine(Application.dataPath, "Settings/OriginalPrefabScales.json");

    // Path to your DataManager's metadata file to see which items exist
    private static string MetadataPath => Path.Combine(Application.dataPath, "Resources/metadata.json");

    [MenuItem("Tools/Prefabs/Apply x0.9 Multiplier to Items")]
    public static void ApplyMultiplier() => ProcessItems(0.9f);

    [MenuItem("Tools/Prefabs/Revert Items to Original")]
    public static void RevertItems() => ProcessItems(1.0f, true);

    private static void ProcessItems(float factor, bool isReverting = false)
    {
        // 1. Load the Scale Database (JSON)
        var scaleDatabase = LoadScaleDatabase();

        // 2. Load the Metadata to get the list of PrefabNames from ItemData
        if (!File.Exists(MetadataPath))
        {
            Debug.LogError($"[ScaleProcessor] Metadata not found at {MetadataPath}. Cannot identify items.");
            return;
        }

        string metadataJson = File.ReadAllText(MetadataPath);
        var metadata = JsonConvert.DeserializeObject<Metadata>(metadataJson);

        if (metadata?.Items == null) return;

        // 3. Get Addressable Settings to find assets by their Addressable Name (PrefabName)
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var entries = settings.groups.SelectMany(g => g.entries).ToList();

        foreach (var item in metadata.Items)
        {
            string prefabAddress = item.PrefabName;
            if (string.IsNullOrEmpty(prefabAddress)) continue;

            // Find the Addressable entry that matches the PrefabName
            var entry = entries.FirstOrDefault(e => e.address == prefabAddress);
            if (entry == null)
            {
                Debug.LogWarning($"[ScaleProcessor] Could not find Addressable asset with address: {prefabAddress}");
                continue;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null) continue;

            // 4. Update Database if this is the first time seeing this GUID
            // Inside ProcessItems loop:
            if (!scaleDatabase.ContainsKey(entry.guid))
            {
                var newData = new ScaleData { AssetGuid = entry.guid };
                newData.SetScale(prefab.transform.localScale); // Use our helper
                scaleDatabase.Add(entry.guid, newData);
            }

            // Applying the math
            Vector3 original = scaleDatabase[entry.guid].OriginalScale;
            prefab.transform.localScale = isReverting ? original : original * factor;

            EditorUtility.SetDirty(prefab);
            Debug.Log($"[ScaleProcessor] Set {prefabAddress} scale to {prefab.transform.localScale}");
        }

        // 6. Finalize
        SaveScaleDatabase(scaleDatabase);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=green><b>Scale processing complete!</b></color>");
    }

    #region JSON Logic
    private static Dictionary<string, ScaleData> LoadScaleDatabase()
    {
        if (!File.Exists(ScaleDbPath)) return new Dictionary<string, ScaleData>();
        return JsonConvert.DeserializeObject<List<ScaleData>>(File.ReadAllText(ScaleDbPath))
                        .ToDictionary(x => x.AssetGuid, x => x);
    }

    private static void SaveScaleDatabase(Dictionary<string, ScaleData> db)
    {
        string dir = Path.GetDirectoryName(ScaleDbPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(ScaleDbPath, JsonConvert.SerializeObject(db.Values.ToList(), Formatting.Indented));
    }
    #endregion
}

[System.Serializable]
public class ScaleData
{
    public string AssetGuid;
    public float ScaleX;
    public float ScaleY;
    public float ScaleZ;

    // Helper to convert back to Vector3
    [JsonIgnore]
    public Vector3 OriginalScale => new Vector3(ScaleX, ScaleY, ScaleZ);

    public void SetScale(Vector3 scale)
    {
        ScaleX = scale.x;
        ScaleY = scale.y;
        ScaleZ = scale.z;
    }
}