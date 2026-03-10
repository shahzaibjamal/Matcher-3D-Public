using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

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
    public static LevelData GenerateDynamicLevel(int levelNumber, List<ItemData> masterItems)
    {
        // 1. Define Growth Curves (Linear or Exponential)
        float difficultyMult = 1f + (levelNumber * 0.15f); // 15% harder per level

        // Metrics
        int targetCount = Mathf.Clamp(3 + (levelNumber / 2), 3, 15); // How many unique types to find
        int itemsPerTarget = Mathf.Clamp(1 + (levelNumber / 5), 1, 5); // How many of each type to find
        int junkCount = Mathf.FloorToInt(10 * difficultyMult); // Background noise

        LevelData newLevel = new LevelData()
        {
            Id = $"Level_{levelNumber}",
            Number = levelNumber,
            Name = $"Challenge {levelNumber}"
        };

        // 2. Select the Targets
        // Shuffle master list and pick 'targetCount' unique items
        var shuffledItems = masterItems.OrderBy(x => UnityEngine.Random.value).ToList();
        var selectedTargets = shuffledItems.Take(targetCount).ToList();

        foreach (var item in selectedTargets)
        {
            // Add to the list of things the player MUST find
            newLevel.ItemsToCollect.Add(item.Id);

            // Add to the physical spawn list
            newLevel.ItemsToSpawn.Add(new LevelItemEntry
            {
                Id = item.Id,
                Count = itemsPerTarget
            });
        }

        // 3. Populate with Junk (Visual Noise)
        // Pick random items that aren't necessarily the targets
        for (int i = 0; i < junkCount; i++)
        {
            var randomJunk = masterItems[UnityEngine.Random.Range(0, masterItems.Count)];

            // Check if item already exists in spawn list to increment count, or add new
            var existing = newLevel.ItemsToSpawn.Find(x => x.Id == randomJunk.Id);
            if (existing != null)
            {
                existing.Count++;
            }
            else
            {
                newLevel.ItemsToSpawn.Add(new LevelItemEntry
                {
                    Id = randomJunk.Id,
                    Count = 1
                });
            }
        }

        // 4. Reward Logic (Basic example: 10 gold per item to collect)
        newLevel.Rewards.Add(new RewardData
        {
            RewardType = RewardType.Gold,
            Amount = newLevel.ItemsToCollect.Count * 10
        });

        return newLevel;
    }
}