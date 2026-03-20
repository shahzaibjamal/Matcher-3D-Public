using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using System.Text;
using System.Collections.Generic;

public static class DataFetcher
{
    private const string WebAppUrl = "https://script.google.com/macros/s/AKfycbyHE731pGDnUfjCTVc3GgbRZ9esCMBQ2_IHkAMaNUuBehT5djLRmk53iXB4J-yhlPABvw/exec";
    private const string SavePath = "Assets/Resources/metadata.json";
    private const string GeneratedScriptPath = "Assets/Scripts/Data/GameConfig.cs";

    [MenuItem("Tools/Sync Google Sheets/1. Fetch Schema (Generate Code)")]
    public static void FetchSchema()
    {
        // Append the mode parameter we set up in Apps Script
        string url = $"{WebAppUrl}?mode=schema";

        EditorUtility.DisplayProgressBar("Google Sheets", "Generating C# Schema...", 0.5f);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var op = request.SendWebRequest();
            while (!op.isDone) { }

            EditorUtility.ClearProgressBar();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GenerateCSFile(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[DataFetcher] Schema Error: {request.error}");
            }
        }
    }

    [MenuItem("Tools/Sync Google Sheets/2. Fetch Metadata (Values)")]
    public static void FetchData()
    {
        EditorUtility.DisplayProgressBar("Google Sheets", "Fetching latest metadata...", 0.2f);

        using (UnityWebRequest request = UnityWebRequest.Get(WebAppUrl)) // No params = default data
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone) { }

            EditorUtility.ClearProgressBar();

            if (request.result == UnityWebRequest.Result.Success)
            {
                SaveMetadata(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[DataFetcher] Network Error: {request.error}");
                EditorUtility.DisplayDialog("Fetch Failed", "Could not connect to Google Sheets.", "OK");
            }
        }
    }

    private static void GenerateCSFile(string json)
    {
        try
        {
            JObject data = JObject.Parse(json);
            JArray settings = (JArray)data["Settings"];

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED CODE - DO NOT MODIFY");
            sb.AppendLine("using System;");
            sb.AppendLine("");
            sb.AppendLine("[Serializable]");
            sb.AppendLine("public class GameConfig");
            sb.AppendLine("{");

            foreach (var item in settings)
            {
                string type = item["type"].ToString();
                string name = item["name"].ToString();
                sb.AppendLine($"    public {type} {name};");
            }

            sb.AppendLine("}");

            string directory = Path.GetDirectoryName(GeneratedScriptPath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(GeneratedScriptPath, sb.ToString());
            AssetDatabase.ImportAsset(GeneratedScriptPath);
            AssetDatabase.Refresh();

            Debug.Log($"<b>[DataFetcher]</b> Schema Generated: {GeneratedScriptPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataFetcher] Schema Generation Error: {e.Message}");
        }
    }

    private static void SaveMetadata(string json)
    {
        try
        {
            // We use JObject to ensure we can save the 'Settings' values even before 
            // the Metadata class is fully recompiled with the new variables
            var data = JsonConvert.DeserializeObject<Metadata>(json);

            if (data == null) return;

            string directory = Path.GetDirectoryName(SavePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            File.WriteAllText(SavePath, JsonConvert.SerializeObject(data, Formatting.Indented));

            AssetDatabase.ImportAsset(SavePath);
            AssetDatabase.Refresh();

            Debug.Log($"<b>[DataFetcher]</b> Metadata values updated: {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataFetcher] Parsing Error: {e.Message}");
        }
    }
}