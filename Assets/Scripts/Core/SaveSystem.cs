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
        GameSaveData data = null;

        // 1. Try to load existing player save
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                data = JsonConvert.DeserializeObject<GameSaveData>(json);

                // Check if the file was empty or mangled into a null object
                if (data == null) throw new System.Exception("File was empty or invalid JSON.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Save file corrupted at {SavePath}! Error: {e.Message}. Falling back to defaults.");
                // We don't return yet; we let it fall through to the fallback logic
                data = null;
            }
        }

        // 2. If no data (file missing OR corrupted), load from Resources
        if (data == null)
        {
            TextAsset defaultAsset = Resources.Load<TextAsset>(DefaultDataResourcePath);
            if (defaultAsset != null)
            {
                try
                {
                    data = JsonConvert.DeserializeObject<GameSaveData>(defaultAsset.text);
                }
                catch
                {
                    Debug.LogError("Designer's DefaultSaveData in Resources is ALSO corrupted!");
                }
            }
        }

        // 3. Ultimate Fallback & Auto-Save
        if (data == null)
        {
            data = new GameSaveData(); // Empty constructor with default values
        }

        // NEW: If we reached this point and there's no physical file (or it was broken), 
        // save the valid 'data' object immediately to "repair" the save path.
        if (!File.Exists(SavePath))
        {
            Save(data);
        }

        return data;
    }
    // Optional: Helper to reset the game to defaults
    public static void ClearSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }
}