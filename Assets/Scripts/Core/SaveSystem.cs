using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "gamedata.json");
    private const string DefaultDataResourcePath = "DefaultSaveData"; // No extension needed for Resources.Load

    public static void Save(GameSaveData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
    }

    public static GameSaveData Load()
    {
        // 1. Check for existing player save
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            return JsonConvert.DeserializeObject<GameSaveData>(json);
        }

        // 2. Fallback to Designer's Default Data in Resources
        TextAsset defaultDataAsset = Resources.Load<TextAsset>(DefaultDataResourcePath);

        if (defaultDataAsset != null)
        {
            Debug.Log("No save found. Initializing from DefaultSaveData template.");
            return JsonConvert.DeserializeObject<GameSaveData>(defaultDataAsset.text);
        }

        // 3. Ultimate Fallback (if designer deleted the file by accident)
        Debug.LogWarning("DefaultSaveData.json missing from Resources! Creating empty object.");
        return new GameSaveData();
    }

    // Optional: Helper to reset the game to defaults
    public static void ClearSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }
}