using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "gamedata.json");

    public static void Save(GameSaveData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
    }

    public static GameSaveData Load()
    {
        if (!File.Exists(SavePath)) return new GameSaveData();

        string json = File.ReadAllText(SavePath);
        return JsonConvert.DeserializeObject<GameSaveData>(json);
    }
}