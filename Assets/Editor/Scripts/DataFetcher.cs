using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO;
using System;

public static class DataFetcher
{
    private const string WebAppUrl = "https://script.google.com/macros/s/AKfycbyHE731pGDnUfjCTVc3GgbRZ9esCMBQ2_IHkAMaNUuBehT5djLRmk53iXB4J-yhlPABvw/exec";
    private const string SavePath = "Assets/Resources/metadata.json";

    [MenuItem("Tools/Fetch MetaData")]
    public static void FetchData()
    {
        EditorUtility.DisplayProgressBar("Google Sheets", "Fetching latest metadata...", 0.2f);

        using (UnityWebRequest request = UnityWebRequest.Get(WebAppUrl))
        {
            var operation = request.SendWebRequest();

            // Wait for request to complete (Editor synchronous block)
            while (!operation.isDone) { }

            EditorUtility.ClearProgressBar();

            if (request.result == UnityWebRequest.Result.Success)
            {
                SaveMetadata(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"[DataFetcher] Network Error: {request.error}");
                EditorUtility.DisplayDialog("Fetch Failed", "Could not connect to Google Sheets. Check your URL and Internet.", "OK");
            }
        }
    }

    private static void SaveMetadata(string json)
    {
        try
        {
            // Deserialize to validate the structure before overwriting local file
            var data = JsonConvert.DeserializeObject<Metadata>(json);

            if (data == null)
            {
                Debug.LogError("[DataFetcher] Received empty data.");
                return;
            }

            // Ensure directory exists
            string directory = Path.GetDirectoryName(SavePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            // Write to Resources folder
            string formattedJson = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(SavePath, formattedJson);

            // Refresh AssetDatabase so Unity sees the change immediately
            AssetDatabase.ImportAsset(SavePath);
            AssetDatabase.Refresh();

            Debug.Log($"<b>[DataFetcher]</b> Successfully updated: {SavePath}");
            // EditorUtility.DisplayDialog("Success", $"Metadata updated!\nLevels: {data.Levels.Count}\nItems: {data.Items.Count}", "Done");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataFetcher] Parsing Error: {e.Message}");
            EditorUtility.DisplayDialog("Parsing Error", "The data from Google Sheets is malformed. Check the console.", "OK");
        }
    }
}