using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class LevelPipelineEditor : Editor
{
    // --- TWEAKABLE SETTINGS ---
    private const int MinItems = 6;      // Level 1 total objects (e.g., 2 types * 3)
    private const int MaxItems = 90;     // Level 35 total objects
    private const int MinVariety = 2;    // Level 1 unique item types
    private const int MaxVariety = 12;   // Level 35 unique item types
    private const int MaxLevels = 35;

    [MenuItem("Tools/Pipeline/Generate and Export to Sheets")]
    public static void GenerateAndExport()
    {
        string metadataPath = Path.Combine(Application.dataPath, "Resources/metadata.json");
        string exportPath = Path.Combine(Application.dataPath, "LevelExport_ForSheets.txt");

        if (!File.Exists(metadataPath)) { Debug.LogError("metadata.json not found!"); return; }

        string json = File.ReadAllText(metadataPath);
        var data = JsonConvert.DeserializeObject<Metadata>(json);

        data.Levels = new List<LevelData>();
        for (int i = 1; i <= MaxLevels; i++)
        {
            data.Levels.Add(CalculateBudgetedLevel(i, data.Items));
        }

        File.WriteAllText(metadataPath, JsonConvert.SerializeObject(data, Formatting.Indented));

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Id\tNumber\tName\tItemsToSpawn\tItemQuantities\tItemsToCollect\tRewards\tRewardQuantities");

        foreach (var lv in data.Levels)
        {
            string spawnIds = "\"" + string.Join(",\n", lv.ItemsToSpawn.Select(x => x.Id)) + "\"";
            string spawnQtys = "\"" + string.Join(",\n", lv.ItemsToSpawn.Select(x => x.Count)) + "\"";
            string collectIds = "\"" + string.Join(",\n", lv.ItemsToCollect) + "\"";
            string rewardTypes = "\"" + string.Join(",\n", lv.Rewards.Select(x => x.RewardType.ToString())) + "\"";
            string rewardQtys = "\"" + string.Join(",\n", lv.Rewards.Select(x => x.Amount)) + "\"";

            sb.Append($"{lv.Id}\t{lv.Number}\t{lv.Name}\t{spawnIds}\t{spawnQtys}\t{collectIds}\t{rewardTypes}\t{rewardQtys}");
            sb.AppendLine();
        }

        File.WriteAllText(exportPath, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>GENERATION COMPLETE:</color> Normalized scaling applied (Max Items: {MaxItems})");
    }

    private static LevelData CalculateBudgetedLevel(int lv, List<ItemData> master)
    {
        // 1. NORMALIZE DIFFICULTY (0.0 to 1.0)
        float t = (float)(lv - 1) / (MaxLevels - 1);

        // Use a curve if you want levels to stay easy longer, then spike at the end
        // float curvedT = t * t; // Exponential growth
        float curvedT = t;      // Linear growth

        // 2. CALCULATE TARGETS BASED ON LERP
        int targetTotalItems = Mathf.RoundToInt(Mathf.Lerp(MinItems, MaxItems, curvedT));
        int targetVariety = Mathf.RoundToInt(Mathf.Lerp(MinVariety, MaxVariety, curvedT));
        int targetCollectables = Mathf.RoundToInt(Mathf.Lerp(1, 6, curvedT));

        // Round targetTotalItems to nearest multiple of 3
        targetTotalItems = (targetTotalItems / 3) * 3;

        LevelData data = new LevelData
        {
            Id = $"level_{lv:D2}",
            Number = lv,
            Name = $"Level {lv:D2}"
        };

        // 3. SELECT POOL
        List<ItemData> pool = master.OrderBy(x => UnityEngine.Random.value).Take(targetVariety).ToList();

        // 4. FILL LOGIC
        int currentCount = 0;
        // First, ensure every item in the variety pool has at least 3
        foreach (var item in pool)
        {
            data.ItemsToSpawn.Add(new LevelItemEntry { Id = item.Id, Count = 3 });
            currentCount += 3;
        }

        // Then, top up random items from the pool until we hit the targetTotalItems
        int safety = 0;
        while (currentCount < targetTotalItems && safety < 500)
        {
            safety++;
            var randomEntry = data.ItemsToSpawn[UnityEngine.Random.Range(0, data.ItemsToSpawn.Count)];
            randomEntry.Count += 3;
            currentCount += 3;
        }

        // 5. ASSIGN COLLECTABLES
        // Take unique IDs from the spawn list for targets
        data.ItemsToCollect = data.ItemsToSpawn
            .Select(x => x.Id)
            .OrderBy(x => UnityEngine.Random.value)
            .Take(targetCollectables)
            .ToList();

        // 6. REWARDS
        int goldAmount = Mathf.FloorToInt(50 + (15 * Mathf.Sqrt(lv)));
        data.Rewards.Add(new RewardData { RewardType = RewardType.Gold, Amount = goldAmount });
        HandlePowerupRewards(lv, data);

        return data;
    }

    private static void HandlePowerupRewards(int lv, LevelData data)
    {
        if (lv == 1) data.Rewards.Add(new RewardData { RewardType = RewardType.Undo, Amount = 5 });
        else if (lv == 2) data.Rewards.Add(new RewardData { RewardType = RewardType.Hint, Amount = 3 });
        else if (lv == 3) data.Rewards.Add(new RewardData { RewardType = RewardType.Shake, Amount = 2 });
        else if (lv == 4) data.Rewards.Add(new RewardData { RewardType = RewardType.Magnet, Amount = 1 });
        else if (lv % 5 == 0)
        {
            RewardType randomType = (RewardType)UnityEngine.Random.Range(2, 6);
            int amount = 1;
            switch (randomType)
            {
                case RewardType.Undo: amount = UnityEngine.Random.Range(1, 6); break;
                case RewardType.Hint: amount = UnityEngine.Random.Range(1, 4); break;
                case RewardType.Shake: amount = UnityEngine.Random.Range(1, 3); break;
                case RewardType.Magnet: amount = 1; break;
            }
            data.Rewards.Add(new RewardData { RewardType = randomType, Amount = amount });
        }
    }
}